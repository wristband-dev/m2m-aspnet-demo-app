using Microsoft.Extensions.Options;

namespace Wristband.AspNet.Auth.M2M;

/// <summary>
/// Provides extension methods for configuring Wristband M2M auth services.
/// </summary>
public static class WristbandM2MAuthExtensions
{
    /// <summary>
    /// Registers Wristband M2M authentication services.
    /// Configures authentication options and adds the M2M authentication service to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to which the authentication services are added.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="WristbandM2MAuthOptions"/>.</param>
    /// <param name="httpClientFactory">Optional external HTTP client factory. If not provided, the existing factory from
    /// DI will be used, or an internal factory will be created.</param>
    /// <returns>The modified <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddWristbandM2MAuth(
        this IServiceCollection services,
        Action<WristbandM2MAuthOptions> configureOptions,
        IHttpClientFactory? httpClientFactory = null)
    {
        services.Configure(configureOptions);

        // Register the M2M auth service with the provided factory or let it resolve from DI
        if (httpClientFactory != null)
        {
            // Use the explicitly provided factory
            services.AddSingleton<IWristbandM2MAuth>(sp => 
            {
                var options = sp.GetRequiredService<IOptions<WristbandM2MAuthOptions>>();
                return new WristbandM2MAuth(options, httpClientFactory);
            });
        }
        else
        {
            // Let the constructor resolve the factory from DI or create internal
            services.AddSingleton<IWristbandM2MAuth>(sp => 
            {
                var options = sp.GetRequiredService<IOptions<WristbandM2MAuthOptions>>();
                // Try to get factory from DI; null will trigger fallback to internal factory
                var factory = sp.GetService<IHttpClientFactory>();
                return new WristbandM2MAuth(options, factory); 
            });
        }

        services.AddSingleton<IWristbandM2MAuth, WristbandM2MAuth>();
        return services;
    }
}
