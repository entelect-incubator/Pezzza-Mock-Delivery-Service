namespace MockDelivery.Api.Workers;

using Common.Models;
using MockDelivery.Api.Services;

/// <summary>
/// Background service that simulates delivery progress by transitioning statuses over time
/// </summary>
public sealed class DeliverySimulationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DeliverySimulationWorker> _logger;

    public DeliverySimulationWorker(
        IServiceProvider serviceProvider,
        ILogger<DeliverySimulationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Delivery Simulation Worker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<IDeliveryStore>();
                var webhookService = scope.ServiceProvider.GetRequiredService<IWebhookService>();
                var settings = scope.ServiceProvider.GetRequiredService<MockDeliverySettings>();

                var pendingDeliveries = store.GetPendingDeliveries();

                foreach (var delivery in pendingDeliveries)
                {
                    // Check if delivery is marked for failure
                    if (!string.IsNullOrEmpty(delivery.FailureReason) && delivery.Status == DeliveryStatus.Created)
                    {
                        delivery.Status = DeliveryStatus.Failed;
                        store.TryUpdate(delivery);

                        _logger.LogWarning(
                            "Delivery {DeliveryId} failed: {Reason}",
                            delivery.Id,
                            delivery.FailureReason);

                        await webhookService.SendWebhookAsync(delivery, stoppingToken);
                        continue;
                    }

                    // Random failure simulation
                    if (settings.Simulation.FailurePercentage > 0
                        && delivery.Status == DeliveryStatus.OnTheWay
                        && Random.Shared.Next(100) < settings.Simulation.FailurePercentage)
                    {
                        delivery.Status = DeliveryStatus.Failed;
                        delivery.FailureReason = "Delivery failed due to unforeseen circumstances";
                        store.TryUpdate(delivery);

                        _logger.LogWarning(
                            "Delivery {DeliveryId} randomly failed (simulation)",
                            delivery.Id);

                        await webhookService.SendWebhookAsync(delivery, stoppingToken);
                        continue;
                    }

                    // Normal status progression
                    var timeInCurrentStatus = DateTime.UtcNow - GetStatusTimestamp(delivery);
                    var transitionDelay = TimeSpan.FromSeconds(settings.Simulation.StatusTransitionDelaySeconds);

                    if (timeInCurrentStatus < transitionDelay)
                        continue;

                    var previousStatus = delivery.Status;
                    var transitioned = TransitionStatus(delivery);

                    if (transitioned)
                    {
                        store.TryUpdate(delivery);

                        _logger.LogInformation(
                            "Delivery {DeliveryId} transitioned from {PreviousStatus} to {NewStatus}",
                            delivery.Id,
                            previousStatus,
                            delivery.Status);

                        await webhookService.SendWebhookAsync(delivery, stoppingToken);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in delivery simulation worker");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("Delivery Simulation Worker stopping");
    }

    private static bool TransitionStatus(Delivery delivery)
    {
        return delivery.Status switch
        {
            DeliveryStatus.Created => TransitionToPickedUp(delivery),
            DeliveryStatus.PickedUp => TransitionToOnTheWay(delivery),
            DeliveryStatus.OnTheWay => TransitionToDelivered(delivery),
            _ => false
        };
    }

    private static bool TransitionToPickedUp(Delivery delivery)
    {
        delivery.Status = DeliveryStatus.PickedUp;
        delivery.PickedUpAt = DateTime.UtcNow;
        return true;
    }

    private static bool TransitionToOnTheWay(Delivery delivery)
    {
        delivery.Status = DeliveryStatus.OnTheWay;
        return true;
    }

    private static bool TransitionToDelivered(Delivery delivery)
    {
        delivery.Status = DeliveryStatus.Delivered;
        delivery.DeliveredAt = DateTime.UtcNow;
        return true;
    }

    private static DateTime GetStatusTimestamp(Delivery delivery)
    {
        return delivery.Status switch
        {
            DeliveryStatus.Created => delivery.CreatedAt,
            DeliveryStatus.PickedUp => delivery.PickedUpAt ?? delivery.CreatedAt,
            DeliveryStatus.OnTheWay => delivery.PickedUpAt ?? delivery.CreatedAt,
            _ => delivery.CreatedAt
        };
    }
}
