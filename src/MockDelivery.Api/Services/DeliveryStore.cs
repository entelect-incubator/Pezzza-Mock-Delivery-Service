namespace MockDelivery.Api.Services;

using Common.Models;
using System.Collections.Concurrent;

/// <summary>
/// In-memory storage for deliveries - thread-safe for concurrent access
/// </summary>
public interface IDeliveryStore
{
    bool TryAdd(Delivery delivery);
    Delivery? GetById(string id);
    Delivery? GetByIdempotencyKey(string key);
    bool TryUpdate(Delivery delivery);
    IReadOnlyList<Delivery> GetAll();
    IReadOnlyList<Delivery> GetPendingDeliveries();
}

public sealed class DeliveryStore : IDeliveryStore
{
    private readonly ConcurrentDictionary<string, Delivery> _deliveries = new();
    private readonly ConcurrentDictionary<string, string> _idempotencyKeys = new();

    public bool TryAdd(Delivery delivery)
    {
        if (!_deliveries.TryAdd(delivery.Id, delivery))
            return false;

        if (!string.IsNullOrEmpty(delivery.IdempotencyKey))
        {
            _idempotencyKeys.TryAdd(delivery.IdempotencyKey, delivery.Id);
        }

        return true;
    }

    public Delivery? GetById(string id)
    {
        _deliveries.TryGetValue(id, out var delivery);
        return delivery;
    }

    public Delivery? GetByIdempotencyKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        if (_idempotencyKeys.TryGetValue(key, out var deliveryId))
        {
            return GetById(deliveryId);
        }

        return null;
    }

    public bool TryUpdate(Delivery delivery)
    {
        return _deliveries.TryUpdate(delivery.Id, delivery, _deliveries[delivery.Id]);
    }

    public IReadOnlyList<Delivery> GetAll()
    {
        return _deliveries.Values.ToList();
    }

    public IReadOnlyList<Delivery> GetPendingDeliveries()
    {
        return _deliveries.Values
            .Where(d => d.Status != DeliveryStatus.Delivered
                     && d.Status != DeliveryStatus.Failed
                     && d.Status != DeliveryStatus.Cancelled)
            .ToList();
    }
}
