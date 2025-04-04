using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace Wristband.AspNet.Auth.M2M;

/// <summary>
/// Interface for Wristband machine-to-machine authentication services.
/// Provides methods to retrieve and manage access tokens for M2M authentication.
/// </summary>
public interface IWristbandM2MAuth : IDisposable
{
    /// <summary>
    /// Retrieves an access token for machine-to-machine authentication.
    /// </summary>
    /// <returns>A task that resolves to a valid access token string.</returns>
    Task<string> GetTokenAsync();
    /// <summary>
    /// Clears the currently cached access token.
    /// </summary>
    void ClearToken();
}

/// <summary>
/// Provides token acquisition, caching, automatic refresh, and background refresh for Wristband M2M authentication.
/// </summary>
public class WristbandM2MAuth : IWristbandM2MAuth
{
    // Lazy-initialized HTTP client factory for creating token client instances.
    private static readonly Lazy<IHttpClientFactory> _factory = new Lazy<IHttpClientFactory>(() => 
        CreateInternalFactory());
    // Reusable form content for client credentials grant type requests.
    private static readonly FormUrlEncodedContent ClientCredentialsContent = new(
    [
        new KeyValuePair<string, string>("grant_type", "client_credentials"),
    ]);

    // Maximum number of attempts to refresh a token before failing.
    private const int MaxRefreshAttempts = 3;
    // Delay in milliseconds between token refresh retry attempts.
    private const int RetryDelayMs = 100;
    // HTTP client for making token requests to the Wristband token endpoint.
    private readonly HttpClient _tokenClient;
    // Configuration options for the Wristband M2M authentication service.
    private readonly WristbandM2MAuthOptions _options;
    // The currently cached access token; empty when no valid token exists.
    private string _cachedToken = string.Empty;
    // Expiration time of the currently cached token in UTC.
    private DateTime _tokenExpiry = DateTime.MinValue;
    // Semaphore to prevent concurrent token refresh operations.
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    // Cancellation token source for the background refresh task.
    private readonly CancellationTokenSource _cts = new();

    // Background task that periodically refreshes the token, if configured.
    private readonly Task? _backgroundRefreshTask;

    /// <summary>
    /// Initializes a new instance of the WristbandM2MAuth class with the specified options.
    /// Sets up HTTP client with authentication headers and starts a background token refresh task, if configured.
    /// </summary>
    /// <param name="options">The options for configuring the Wristband M2M authentication service.</param>
    /// <param name="httpClientFactory">Optional external HTTP client factory. If not provided, an internal factory will be used.</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    /// <exception cref="ArgumentException">Thrown when required options are missing or invalid.</exception>
    public WristbandM2MAuth(IOptions<WristbandM2MAuthOptions> options, IHttpClientFactory? httpClientFactory = null)
    {
        // Validate configuration options.
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        ValidateOptions(_options);

        // Use the provided factory, or fall back to internal one otherwise.
        var factory = httpClientFactory ?? _factory.Value;

        // Use Http client factory to create a token endpoint client
        _tokenClient = _factory.Value.CreateClient("WristbandM2MAuth");
        _tokenClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _tokenClient.Timeout = TimeSpan.FromSeconds(30);

        // Add a Basic authentication header
        var credentialsBytes = System.Text.Encoding.UTF8.GetBytes($"{_options.ClientId!}:{_options.ClientSecret!}");
        var base64Credentials = Convert.ToBase64String(credentialsBytes);
        _tokenClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Credentials);

        // If a valid BackgroundTokenRefreshInterval is provided, start the background refresh loop.
        if (_options.BackgroundTokenRefreshInterval.HasValue)
        {
            // Start background refresh loop with the configured interval
            _backgroundRefreshTask = Task.Run(() => BackgroundTokenRefreshLoop(_options.BackgroundTokenRefreshInterval.Value), _cts.Token);
        }
    }

    /// <summary>
    /// Retrieves an access token for the machine-to-machine client. The access token is cached in memory for subsequent calls.
    /// When the access token expires, this method will automatically get a new access token when it's called.
    /// </summary>
    public async Task<string> GetTokenAsync()
    {
        await RefreshTokenAsync();
        return _cachedToken;
    }

    /// <summary>
    /// Clears the access token from the cache
    /// </summary>
    public void ClearToken()
    {
        Console.WriteLine("[M2M AUTH] Clearing cached M2M token");
        _cachedToken = string.Empty;
        _tokenExpiry = DateTime.MinValue;
    }

    /// <summary>
    /// Disposes resources used by this instance, including canceling any background refresh tasks.
    /// </summary>
    public void Dispose()
    {
        // Cancel the token source
        _cts.Cancel();

        // If the background task was created, wait for it to complete
        if (_backgroundRefreshTask != null)
        {
            _backgroundRefreshTask.Wait();
        }

        // Clean up any other resources if necessary
        _cts.Dispose();
    }

    /// <summary>
    /// Refreshes the access token if needed with retry logic for server errors.
    /// Makes up to MaxRefreshAttempts to refresh the token with RetryDelayMs delay between attempts.
    /// Only retries on 5xx server errors, fails immediately on 4xx client errors.
    /// </summary>
    /// <returns>A task that completes when the token has been refreshed or determined to be valid.</returns>
    /// <exception cref="Exception">Thrown when all refresh attempts fail or when a client error (4xx) occurs.</exception>
    private async Task RefreshTokenAsync()
    {
        for (int attempt = 1; attempt <= MaxRefreshAttempts; attempt++)
        {
            bool semaphoreAcquired = false;
            try
            {
                // Check if we have a valid cached token
                if (IsTokenValid())
                {
                    return;
                }

                // Use semaphore to prevent multiple concurrent token requests
                await _semaphore.WaitAsync();
                semaphoreAcquired = true;
                
                // Double-check if token was refreshed by another thread
                if (IsTokenValid())
                {
                    return;
                }
                
                // Make a request for this specific token fetch
                var request = new HttpRequestMessage(HttpMethod.Post, $"https://{_options.ApplicationDomain}/api/v1/oauth2/token");
                request.Content = ClientCredentialsContent;
                var response = await _tokenClient.SendAsync(request);
                
                // This will throw if status code is not success
                response.EnsureSuccessStatusCode();

                var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>() 
                    ?? throw new Exception("Failed to deserialize token response");

                // Get the configured buffer (with a fallback to zero if it's negative)
                TimeSpan expiryBuffer = _options.TokenExpiryBuffer;

                // Cache the token and set expiry based on configured buffer
                var buffer = tokenResponse.ExpiresIn <= expiryBuffer.TotalSeconds ? TimeSpan.FromSeconds(60) : expiryBuffer;
                _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn).Subtract(buffer);
                _cachedToken = tokenResponse.AccessToken;
                Console.WriteLine("[M2M AUTH] Retrieved new M2M token, valid until: " + _tokenExpiry);
                return;
            }
            catch (HttpRequestException ex) when (ex.StatusCode >= HttpStatusCode.InternalServerError && attempt < MaxRefreshAttempts)
            {
                // Only retry for 5xx server errors and if we haven't reached max attempts
                Console.WriteLine($"[M2M AUTH] Server error (5xx) on attempt {attempt}/{MaxRefreshAttempts}, retrying after delay: {ex.Message}");
                await Task.Delay(RetryDelayMs);
            }
            catch (HttpRequestException ex) when (ex.StatusCode >= HttpStatusCode.BadRequest && ex.StatusCode < HttpStatusCode.InternalServerError)
            {
                // Don't retry for 4xx client errors
                Console.WriteLine($"[M2M AUTH] Client error (4xx), not retrying: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                // Unexpected error
                Console.WriteLine("[M2M AUTH] Failed to retrieve M2M token from Wristband: " + ex);
                throw;
            }
            finally
            {
                if (semaphoreAcquired)
                {
                    _semaphore.Release();
                }
            }
        }
        
        // If we get here, we've exhausted all retry attempts
        throw new Exception($"[M2M AUTH] Failed to refresh token after {MaxRefreshAttempts} attempts");
    }

    /// <summary>
    /// Checks if the current token is valid (not empty and not expired).
    /// </summary>
    /// <returns>True if the cached token is valid and not expired, otherwise false.</returns>
    private bool IsTokenValid()
    {
        return !string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry;
    }

    /// <summary>
    /// Background task that periodically refreshes the access token at the specified interval.
    /// Continues running until the service is disposed or an unrecoverable error occurs.
    /// </summary>
    /// <param name="refreshInterval">The interval at which to refresh the token.</param>
    /// <returns>A task that completes when the cancellation token is canceled.</returns>
    private async Task BackgroundTokenRefreshLoop(TimeSpan refreshInterval)
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(refreshInterval, _cts.Token);
                await RefreshTokenAsync();
            }
            catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
            {
                // Exit loop cleanly on shutdown
                Console.WriteLine("[M2M AUTH] Background token refresh loop was canceled.");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[M2M AUTH] Background token refresh failed: {ex}");
            }
        }
    }

    /// <summary>
    /// Validates the provided options to ensure all required values are present and valid.
    /// </summary>
    /// <param name="options">The options to validate.</param>
    /// <exception cref="ArgumentException">Thrown when required options are missing or invalid.</exception>
    private static void ValidateOptions(WristbandM2MAuthOptions options)
    {
        if (string.IsNullOrEmpty(options.ApplicationDomain)){
            throw new ArgumentException("ApplicationDomain is required");
        }
        if (string.IsNullOrEmpty(options.ClientId)){
            throw new ArgumentException("ClientId is required");
        } 
        if (string.IsNullOrEmpty(options.ClientSecret))
        {
            throw new ArgumentException("ClientSecret is required");
        }
        if (options.BackgroundTokenRefreshInterval.HasValue && options.BackgroundTokenRefreshInterval < TimeSpan.FromMinutes(1))
        {
            throw new ArgumentException("BackgroundTokenRefreshInterval must be at least 1 minute");
        }
        if (options.TokenExpiryBuffer < TimeSpan.Zero)
        {
            throw new ArgumentException("TokenExpiryBuffer cannot be negative");
        }
    }

    /// <summary>
    /// Creates an internal HTTP client factory for token requests.
    /// This allows the class to create and configure HTTP clients without external dependencies.
    /// </summary>
    /// <returns>An HTTP client factory configured for Wristband token requests.</returns>
    private static IHttpClientFactory CreateInternalFactory()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("WristbandM2MAuth", client => {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IHttpClientFactory>();
    }
}
