namespace Common.Models;

/// <summary>
/// Represents a delivery request
/// </summary>
public sealed class Delivery
{
    public required string Id { get; init; }

    public required string OrderId { get; init; }

    public required DeliveryAddress PickupAddress { get; init; }

    public required DeliveryAddress DeliveryAddress { get; init; }

    public DeliveryStatus Status { get; set; }

    public string? IdempotencyKey { get; init; }

    public string? WebhookUrl { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? PickedUpAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? FailureReason { get; set; }

    public DeliveryDriver? Driver { get; set; }
}

public sealed class DeliveryAddress
{
    public required string Street { get; init; }

    public required string City { get; init; }

    public required string PostalCode { get; init; }

    public string? Instructions { get; init; }
}

public sealed class DeliveryDriver
{
    public required string Name { get; init; }

    public required string Phone { get; init; }

    public required string VehicleNumber { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DeliveryStatus
{
    Created,
    PickedUp,
    OnTheWay,
    Delivered,
    Failed,
    Cancelled
}
