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
    /// <returns>The modified <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddWristbandM2MAuth(
        this IServiceCollection services,
        Action<WristbandM2MAuthOptions> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddSingleton<IWristbandM2MAuth, WristbandM2MAuth>();
        return services;
    }
}
