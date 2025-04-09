using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols;

namespace Wristband.AspNet.Auth.Jwt;

/// <summary>
/// The <see cref="JwksProvider"/> class is responsible for retrieving and managing JSON Web Key Sets (JWKS)
/// from your Wristband application's domain. It provides the necessary token validation parameters and event handling
/// to validate JWTs issued by Wristband. The JWKS is used to resolve signing keys for token validation.
/// </summary>
public class JwksProvider
{
    private const string JwksApiPath = "/api/v1/oauth2/jwks";

    // JWKS configuration manager that handles caching (default 24h) and retrieval
    private readonly ConfigurationManager<JsonWebKeySet> _configManager;
    private readonly string _issuerDomain;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwksProvider"/> class. Configures the JWKS provider by setting the
    /// issuer domain and initializing the JWKS configuration manager to handle key retrieval and caching.
    /// </summary>
    /// <param name="options">
    /// The <see cref="WristbandJwtValidationOptions"/> used to configure the JWKS provider, including the application
    /// domain for the issuer and the JWKS URI.
    /// </param>
    public JwksProvider(WristbandJwtValidationOptions options)
    {
        _issuerDomain = $"https://{options.ApplicationDomain}";
        _configManager = new ConfigurationManager<JsonWebKeySet>(
            $"{_issuerDomain}{JwksApiPath}",
            new JsonWebKeySetRetriever(),
            new HttpDocumentRetriever { RequireHttps = true });
    }

    /// <summary>
    /// Generates <see cref="TokenValidationParameters"/> based on the JWKS configuration used to validate the JWT
    /// token during authentication.
    /// </summary>
    /// <returns>
    /// Returns <see cref="TokenValidationParameters"/> with settings such as issuer validation, algorithm, and signing
    /// key resolver.
    /// </returns>
    public TokenValidationParameters GetTokenValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _issuerDomain,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            ValidateIssuerSigningKey = true,
            ValidAlgorithms = ["RS256"],
            IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
            {
                var jwks = _configManager.GetConfigurationAsync(CancellationToken.None)
                    .GetAwaiter().GetResult();

                if (!string.IsNullOrEmpty(kid))
                {
                    var key = jwks.Keys.FirstOrDefault(k => k.Kid == kid);
                    if (key != null)
                    {
                        return [key];
                    }
                }

                return jwks.Keys;
            }
        };
    }

    /// <summary>
    /// Creates a set of <see cref="JwtBearerEvents"/> for handling JWT authentication events,
    /// such as logging received bearer tokens and handling authentication failures.
    /// </summary>
    /// <returns>Returns an instance of <see cref="JwtBearerEvents"/> with event handlers for
    /// token reception and authentication failure logging.</returns>
    public JwtBearerEvents GetJwtBearerEvents()
    {
        return new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                Console.WriteLine($"[JWT VALIDATION] Validating bearer token...");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"[JWT VALIDATION] Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            }
        };
    }
}
