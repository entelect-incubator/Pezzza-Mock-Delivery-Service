namespace Common;

[ExcludeFromCodeCoverage]
public sealed class MockDeliverySettings
{
    public string DisplayName { get; set; } = "Pezzza Mock Delivery Service";

    public OpenApiSettings OpenApi { get; set; } = new();

    public SimulationSettings Simulation { get; set; } = new();

    public WebhookSettings Webhook { get; set; } = new();
}

[ExcludeFromCodeCoverage]
public sealed class OpenApiSettings
{
    public string Title { get; set; } = "Mock Delivery API";

    public string Version { get; set; } = "v1";

    public string Description { get; set; } = "A realistic mock delivery service for training and integration testing";

    public bool Enable { get; set; } = true;
}

[ExcludeFromCodeCoverage]
public sealed class SimulationSettings
{
    /// <summary>
    /// Delay in seconds between delivery status transitions
    /// </summary>
    public int StatusTransitionDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Percentage chance (0-100) that a delivery will randomly fail
    /// </summary>
    public int FailurePercentage { get; set; } = 10;

    /// <summary>
    /// Enable random delays to simulate real-world API latency
    /// </summary>
    public bool EnableRandomDelays { get; set; } = true;

    /// <summary>
    /// Maximum random delay in milliseconds
    /// </summary>
    public int MaxRandomDelayMs { get; set; } = 500;
}

[ExcludeFromCodeCoverage]
public sealed class WebhookSettings
{
    /// <summary>
    /// Enable webhook callbacks to notify clients of status changes
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Retry count for failed webhook deliveries
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Timeout in seconds for webhook HTTP requests
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
