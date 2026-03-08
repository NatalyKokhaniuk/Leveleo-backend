using LeveLEO.Data;
using LeveLEO.Features.Inventory.Models;
using LeveLEO.Features.Products.Models;
using Microsoft.EntityFrameworkCore;

namespace LeveLEO.Features.Inventory.Services;

public class InventoryService(AppDbContext db) : IInventoryService
{
    public async Task<int> GetAvailableQuantityAsync(Guid productId)
    {
        var product = await db.Products.FindAsync(productId);
        if (product == null)
            return 0;

        var reserved = await db.InventoryReservations
            .Where(r => r.ProductId == productId && (r.ExpiresAt == null || r.ExpiresAt > DateTimeOffset.UtcNow))
            .SumAsync(r => r.Quantity);

        return Math.Max(0, product.StockQuantity - reserved);
    }

    public async Task<bool> CanReserveAsync(Guid productId, int quantity)
    {
        var available = await GetAvailableQuantityAsync(productId);
        return quantity <= available;
    }

    public async Task ReserveAsync(Guid productId, Guid orderId, int quantity, TimeSpan? ttl = null)
    {
        if (!await CanReserveAsync(productId, quantity))
            throw new InvalidOperationException("Not enough stock to reserve.");

        var reservation = new InventoryReservation
        {
            ProductId = productId,
            OrderId = orderId,
            Quantity = quantity,
            ExpiresAt = ttl.HasValue ? DateTimeOffset.UtcNow.Add(ttl.Value) : (DateTimeOffset?)null
        };

        db.InventoryReservations.Add(reservation);
        await db.SaveChangesAsync();
    }

    public async Task ReleaseAsync(Guid productId, Guid orderId)
    {
        var reservations = await db.InventoryReservations
            .Where(r => r.ProductId == productId && r.OrderId == orderId)
            .ToListAsync();

        if (reservations.Count != 0)
        {
            db.InventoryReservations.RemoveRange(reservations);
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Підтверджує резервування і списує товар зі складу
    /// Викликається після успішної оплати
    /// </summary>
    public async Task ConfirmReservationAsync(Guid productId, Guid orderId)
    {
        var reservation = await db.InventoryReservations
            .FirstOrDefaultAsync(r => r.ProductId == productId && r.OrderId == orderId)
            ?? throw new ApiException(
                "RESERVATION_NOT_FOUND",
                $"Reservation for product {productId} and order {orderId} not found.",
                404
            );

        var product = await db.Products
            .FirstOrDefaultAsync(p => p.Id == productId)
            ?? throw new ApiException(
                "PRODUCT_NOT_FOUND",
                $"Product with Id '{productId}' not found.",
                404
            );

        // Списуємо зі складу
        product.StockQuantity -= reservation.Quantity;

        if (product.StockQuantity < 0)
        {
            throw new ApiException(
                "INSUFFICIENT_STOCK",
                $"Insufficient stock for product {productId}.",
                400
            );
        }

        // Видаляємо резервування
        db.InventoryReservations.Remove(reservation);

        await db.SaveChangesAsync();
    }
}