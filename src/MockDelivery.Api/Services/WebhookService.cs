namespace MockDelivery.Api.Services;

using Common.Models;

/// <summary>
/// Webhook notification service - sends HTTP callbacks when delivery status changes
/// </summary>
public interface IWebhookService
{
    Task SendWebhookAsync(Delivery delivery, CancellationToken cancellationToken = default);
}

public sealed class WebhookService : IWebhookService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MockDeliverySettings _settings;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        IHttpClientFactory httpClientFactory,
        MockDeliverySettings settings,
        ILogger<WebhookService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings;
        _logger = logger;
    }

    public async Task SendWebhookAsync(Delivery delivery, CancellationToken cancellationToken = default)
    {
        if (!_settings.Webhook.Enabled || string.IsNullOrEmpty(delivery.WebhookUrl))
        {
            return;
        }

        var payload = new DeliveryWebhookPayload
        {
            DeliveryId = delivery.Id,
            OrderId = delivery.OrderId,
            Status = delivery.Status,
            Timestamp = DateTime.UtcNow,
            FailureReason = delivery.FailureReason,
            Driver = delivery.Driver
        };

        var retryCount = 0;
        var maxRetries = _settings.Webhook.RetryCount;

        while (retryCount <= maxRetries)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(_settings.Webhook.TimeoutSeconds);

                var response = await client.PostAsJsonAsync(delivery.WebhookUrl, payload, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Webhook sent successfully for delivery {DeliveryId} to {WebhookUrl}",
                        delivery.Id,
                        delivery.WebhookUrl);
                    return;
                }

                _logger.LogWarning(
                    "Webhook failed for delivery {DeliveryId}. Status: {StatusCode}. Retry {Retry}/{MaxRetries}",
                    delivery.Id,
                    response.StatusCode,
                    retryCount,
                    maxRetries);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Webhook exception for delivery {DeliveryId}. Retry {Retry}/{MaxRetries}",
                    delivery.Id,
                    retryCount,
                    maxRetries);
            }

            retryCount++;
            if (retryCount <= maxRetries)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken);
            }
        }

        _logger.LogError(
            "Webhook failed permanently for delivery {DeliveryId} after {MaxRetries} retries",
            delivery.Id,
            maxRetries);
    }
}
