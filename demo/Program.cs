using DotNetEnv;

using Wristband.AspNet.Auth.Jwt;
using Wristband.AspNet.Auth.M2M;

// Load environment variables from .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add Configuration from multiple sources: "appsettings.json" AND ".env".
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// HTTP Context Configuration
builder.Services.AddHttpContextAccessor();

// JSON Configuration
builder.Services.ConfigureHttpJsonOptions(json =>
{
  json.SerializerOptions.WriteIndented = true;
});

// Configure Wristband M2M Auth.
builder.Services.AddWristbandM2MAuth(options =>
{
    options.ApplicationDomain = builder.Configuration["APPLICATION_DOMAIN"];
    options.ClientId = builder.Configuration["CLIENT_ID"];
    options.ClientSecret = builder.Configuration["CLIENT_SECRET"];
    options.BackgroundTokenRefreshInterval = TimeSpan.FromMinutes(30);
});

// Configure Wristband JWT validation with JWKS
builder.Services.AddWristbandJwtValidation(options =>
{
    options.ApplicationDomain = builder.Configuration["APPLICATION_DOMAIN"];
});

// Configure HttpClient for calling the Protected API
builder.Services.AddHttpClient<ProtectedApiClient>();

// Configure authorization to allow usage of RequiresAuthorization() on endpoints.
builder.Services.AddAuthorization();

// Configure to always listen on localhost
builder.WebHost.UseUrls("http://localhost:6001");

var app = builder.Build();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map the Demo API endpoints
app.MapDemoEndpoints();

// Initialize the Wristband client to get the initial token during startup
try
{
    // Load the access token into the cache
    var wristbandM2MAuth = app.Services.GetRequiredService<IWristbandM2MAuth>();
    await wristbandM2MAuth.GetTokenAsync();
}
catch (Exception ex)
{
    Console.WriteLine("[M2M AUTH] Failed to retrieve initial M2M token: " + ex);
}

app.Run();
