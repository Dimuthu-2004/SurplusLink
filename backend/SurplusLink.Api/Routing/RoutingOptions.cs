namespace SurplusLink.Api.Routing;

public sealed class RoutingOptions
{
    public string Provider { get; set; } = "OpenRouteService";
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public double TimeoutSeconds { get; set; } = 10;
    public decimal? BaseFee { get; set; }
    public decimal? CostPerKm { get; set; }
    public decimal? CostPerMinute { get; set; }

    public bool CanRoute => Provider == "OpenRouteService"
        && Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo)
        && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment)
        && !string.IsNullOrWhiteSpace(ApiKey) && !ApiKey.Any(char.IsControl)
        && double.IsFinite(TimeoutSeconds) && TimeoutSeconds > 0 && TimeoutSeconds <= 120;

    public bool CanEstimate => BaseFee is >= 0 && CostPerKm is >= 0 && CostPerMinute is >= 0;
}

public static class RoutingRegistration
{
    public static IServiceCollection AddRoutingProvider(this IServiceCollection services)
    {
        // Deliberately excludes appsettings, user secrets, command-line and client configuration.
        var environment = new ConfigurationBuilder().AddEnvironmentVariables("Routing__").Build();
        var options = new RoutingOptions();
        environment.Bind(options);
        services.AddSingleton(options);
        services.AddHttpClient<IRoutingProvider, OpenRouteServiceRoutingProvider>(client =>
        {
            // The adapter's deadline covers the entire request and response parsing.
            client.Timeout = Timeout.InfiniteTimeSpan;
            client.MaxResponseContentBufferSize = 64 * 1024;
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
        .RedactLoggedHeaders(_ => true);
        services.AddTransient<ITransportEstimateService, TransportEstimateService>();
        return services;
    }
}
