using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Wristband.AspNet.Auth.Jwt;

/// <summary>
/// Provides extension methods for configuring Wristband JWT validation services.
/// </summary>
public static class WristbandJwtValidationExtensions
{
    /// <summary>
    /// Configures JWT validation for Wristband authentication.
    /// This sets up JSON Web Key Set (JWKS) retrieval and configures JWT validation parameters.
    /// </summary>
    /// <param name="services">The service collection to which JWT validation is added.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="WristbandJwtValidationOptions"/>.</param>
    /// <returns>The modified <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddWristbandJwtValidation(
        this IServiceCollection services,
        Action<WristbandJwtValidationOptions> configureOptions)
    {
        var options = new WristbandJwtValidationOptions();
        configureOptions(options);

        var provider = new JwksProvider(options);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwtOptions =>
            {
                jwtOptions.Events = provider.GetJwtBearerEvents();
                jwtOptions.TokenValidationParameters = provider.GetTokenValidationParameters();
            });

        return services;
    }
}
