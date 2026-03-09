using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LeveLEO.Features.Inventory.Services;

public interface IInventoryService
{
    Task<bool> CanReserveAsync(Guid productId, int quantity);

    Task ReserveAsync(Guid productId, Guid orderId, int quantity, TimeSpan? ttl = null);

    Task ReleaseAsync(Guid productId, Guid orderId);

    Task<int> GetAvailableQuantityAsync(Guid productId);

    Task ConfirmReservationAsync(Guid productId, Guid orderId);

    // Реализация по умолчанию, чтобы не требовать добавления абстрактного метода у всех реализаций.
    Task<Dictionary<Guid, int>> GetAvailableQuantitiesAsync(IEnumerable<Guid> productIds);
}