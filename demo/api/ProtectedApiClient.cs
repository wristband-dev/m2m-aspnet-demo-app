using System.Net;
using System.Net.Http.Headers;
using Wristband.AspNet.Auth.M2M;

public class ProtectedApiClient
{
    private readonly HttpClient _client;
    private readonly IWristbandM2MAuth _wristbandM2MAuth;

    public ProtectedApiClient(HttpClient client, IWristbandM2MAuth wristbandM2MAuth)
    {
        // Protected API Client
        _client = client;
        _client.BaseAddress = new Uri("http://localhost:6001");
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Wristband M2M Auth
        _wristbandM2MAuth = wristbandM2MAuth;
    }

    public async Task<string> GetProtectedDataAsync()
    {
        // Get the token from the M2M client (may refresh token if expired)
        var token = await _wristbandM2MAuth.GetTokenAsync();

        // Attach Bearer token to request
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/protected/data");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        // Call the Protected API
        var response = await _client.SendAsync(request);

        // Clear the token cache for any unauthorized errors
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _wristbandM2MAuth.ClearToken();
        }

        // Ensure success and return response data
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
