namespace Common.Models;

/// <summary>
/// Request to create a new delivery
/// </summary>
public sealed class CreateDeliveryRequest
{
    public required string OrderId { get; init; }

    public required DeliveryAddress PickupAddress { get; init; }

    public required DeliveryAddress DeliveryAddress { get; init; }

    public string? IdempotencyKey { get; init; }

    public string? WebhookUrl { get; init; }

    /// <summary>
    /// Set to true to force this delivery to fail (for testing)
    /// </summary>
    public bool ForceFail { get; init; }
}

/// <summary>
/// Response after creating a delivery
/// </summary>
public sealed class CreateDeliveryResponse
{
    public required string DeliveryId { get; init; }

    public required DeliveryStatus Status { get; init; }

    public required DateTime CreatedAt { get; init; }

    public string? Message { get; init; }
}

/// <summary>
/// Response for delivery status query
/// </summary>
public sealed class DeliveryResponse
{
    public required string DeliveryId { get; init; }

    public required string OrderId { get; init; }

    public required DeliveryStatus Status { get; init; }

    public required DateTime CreatedAt { get; init; }

    public DateTime? PickedUpAt { get; init; }

    public DateTime? DeliveredAt { get; init; }

    public DateTime? CancelledAt { get; init; }

    public string? FailureReason { get; init; }

    public DeliveryDriver? Driver { get; init; }

    public DeliveryAddress? DeliveryAddress { get; init; }
}

/// <summary>
/// Request to cancel a delivery
/// </summary>
public sealed class CancelDeliveryRequest
{
    public string? Reason { get; init; }
}

/// <summary>
/// Response after cancelling a delivery
/// </summary>
public sealed class CancelDeliveryResponse
{
    public required string DeliveryId { get; init; }

    public required DeliveryStatus Status { get; init; }

    public required DateTime CancelledAt { get; init; }

    public string? Message { get; init; }
}

/// <summary>
/// Webhook payload sent to clients when delivery status changes
/// </summary>
public sealed class DeliveryWebhookPayload
{
    public required string DeliveryId { get; init; }

    public required string OrderId { get; init; }

    public required DeliveryStatus Status { get; init; }

    public required DateTime Timestamp { get; init; }

    public string? FailureReason { get; init; }

    public DeliveryDriver? Driver { get; init; }
}
