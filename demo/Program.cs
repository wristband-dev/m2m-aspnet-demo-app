using DotNetEnv;

using Wristband.AspNet.Auth.Jwt;
using Wristband.AspNet.Auth.M2M;

// -----------------------------------------------------------------------------
// Environment & configuration
// -----------------------------------------------------------------------------

// Load environment variables from .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add Configuration from multiple sources: "appsettings.json" AND ".env".
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// -----------------------------------------------------------------------------
// Core infrastructure services
// -----------------------------------------------------------------------------

// HTTP Context Configuration
builder.Services.AddHttpContextAccessor();

// JSON Configuration
builder.Services.ConfigureHttpJsonOptions(json =>
{
  json.SerializerOptions.WriteIndented = true;
});

// -----------------------------------------------------------------------------
// Authentication & authorization
// -----------------------------------------------------------------------------

// Configure Wristband M2M Auth.
builder.Services.AddWristbandM2MAuth(options =>
{
    options.WristbandApplicationDomain = builder.Configuration["APPLICATION_VANITY_DOMAIN"];
    options.ClientId = builder.Configuration["CLIENT_ID"];
    options.ClientSecret = builder.Configuration["CLIENT_SECRET"];
    options.BackgroundTokenRefreshInterval = TimeSpan.FromHours(1);
    options.TokenExpiryBuffer = TimeSpan.FromMinutes(5);
});

// Register JWT Bearer authentication with Wristband JWKS validation
builder.Services.AddAuthentication()
    .AddJwtBearer(options => options.UseWristbandJwksValidation(
        wristbandApplicationVanityDomain: builder.Configuration["APPLICATION_VANITY_DOMAIN"]!
    ));

// Configure authorization and register the WristbandJwt policy
builder.Services.AddAuthorization(options => options.AddWristbandJwtPolicy());

// -----------------------------------------------------------------------------
// HTTP clients & app services
// -----------------------------------------------------------------------------

// Configure HttpClient for calling the Protected API
builder.Services.AddHttpClient<ProtectedApiClient>();

// -----------------------------------------------------------------------------
// Host configuration
// -----------------------------------------------------------------------------

// Listen on localhost port 6001
builder.WebHost.UseUrls("http://localhost:6001");

// -----------------------------------------------------------------------------
// Build application
// -----------------------------------------------------------------------------

var app = builder.Build();

// -----------------------------------------------------------------------------
// Middleware pipeline
// -----------------------------------------------------------------------------

app.UseAuthentication();
app.UseAuthorization();

// -----------------------------------------------------------------------------
// Endpoints
// -----------------------------------------------------------------------------

app.MapDemoEndpoints();

// -----------------------------------------------------------------------------
// Startup warmup
// -----------------------------------------------------------------------------

try
{
    // Initialize the Wristband client to get and cache the initial token during startup
    var wristbandM2MAuth = app.Services.GetRequiredService<IWristbandM2MAuthService>();
    await wristbandM2MAuth.GetTokenAsync();
}
catch (Exception ex)
{
    Console.WriteLine("[M2M AUTH] Failed to retrieve initial M2M token: " + ex);
}

// -----------------------------------------------------------------------------
// Run
// -----------------------------------------------------------------------------

app.Run();
