using LeveLEO.Data;
using LeveLEO.Features.Inventory;
using LeveLEO.Features.Inventory.Services;
using LeveLEO.Features.Orders.DTO;
using LeveLEO.Features.Orders.Models;
using LeveLEO.Features.Payments.DTO;
using LeveLEO.Features.Payments.Models;
using LeveLEO.Features.Payments.Services;
using LeveLEO.Features.Products.DTO;
using LeveLEO.Features.Products.Services;
using LeveLEO.Features.Shipping.DTO;
using LeveLEO.Features.Shipping.Models;
using LeveLEO.Features.Shipping.Services;
using LeveLEO.Features.ShoppingCarts;
using LeveLEO.Features.ShoppingCarts.DTO;
using LeveLEO.Features.ShoppingCarts.Services;
using LeveLEO.Infrastructure.Payments;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace LeveLEO.Features.Orders.Services;

public class OrderService(
    AppDbContext db,
    IShoppingCartService cartService,
    IInventoryService inventoryService,
    IPaymentService paymentService,
    IProductService productService,
    IAddressService addressService) : IOrderService
{
    private static readonly TimeSpan PayloadValidity = TimeSpan.FromMinutes(10);

    public async Task<OrderDetailDto> GetByIdAsync(Guid orderId)
    {
        var order = await db.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.Address)
            .Include(o => o.Payment)
            .Include(o => o.Delivery)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new ApiException(
                "ORDER_NOT_FOUND",
                $"Order with Id '{orderId}' not found.",
                404
            );

        // Оновлюємо статус платежу, якщо потрібно
        await RefreshPaymentStatusIfNeededAsync(order);

        // Якщо платіж є — підтягуємо свіжі дані (опціонально, якщо потрібно)
        if (order.PaymentId != null)
        {
            _ = await paymentService.GetPaymentByIdAsync((Guid)order.PaymentId);
        }

        return await MapToDetailDtoAsync(order);
    }

    public async Task<OrderDetailDto> GetByNumberAsync(string orderNumber)
    {
        var order = await db.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.Address)
            .Include(o => o.Payment)
            .Include(o => o.Delivery)
            .FirstOrDefaultAsync(o => o.Number == orderNumber)
            ?? throw new ApiException(
                "ORDER_NOT_FOUND",
                $"Order with Number '{orderNumber}' not found.",
                404
            );

        // Оновлюємо статус платежу, якщо потрібно
        await RefreshPaymentStatusIfNeededAsync(order);

        if (order.PaymentId != null)
        {
            _ = await paymentService.GetPaymentByIdAsync((Guid)order.PaymentId);
        }

        return await MapToDetailDtoAsync(order);
    }

    public async Task<List<OrderListItemDto>> GetByUserIdAsync(string userId, DateTimeOffset? startDate, DateTimeOffset? endDate)
    {
        var query = db.Orders
            .Where(o => o.UserId == userId && o.Status != OrderStatus.PaymentFailed);

        if (startDate.HasValue)
            query = query.Where(o => o.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(o => o.CreatedAt <= endDate.Value);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(); // спочатку завантажуємо повні об’єкти

        // Оновлюємо статус платежів для всіх знайдених замовлень
        foreach (var order in orders)
        {
            await RefreshPaymentStatusIfNeededAsync(order);
        }

        // Тепер мапимо в DTO після можливого оновлення
        return [.. orders.Select(o => new OrderListItemDto
        {
            Id = o.Id,
            Number = o.Number,
            CreatedAt = o.CreatedAt,
            Status = o.Status,
            TotalPayable = o.TotalPayable,
            AddressSummary = $"{o.Address?.CityName}, {o.Address?.Street} {o.Address?.House}"
        })];
    }

    public async Task<OrderDetailDto> UpdateAsync(Guid orderId, OrderUpdateDto orderUpdate)
    {
        var order = await db.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.Address)
            .Include(o => o.Payment)
            .Include(o => o.Delivery)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new ApiException(
                "ORDER_NOT_FOUND",
                $"Order with Id '{orderId}' not found.",
                404
            );

        if (orderUpdate.Status.HasValue)
        { order.Status = orderUpdate.Status.Value; }
        if (orderUpdate.AddressId.HasValue)
        {
            var addressExists = await db.UserAddresses
        .AnyAsync(ua => ua.AddressId == orderUpdate.AddressId.Value && ua.UserId == order.UserId);

            if (!addressExists)
                throw new ApiException(
                    "ADDRESS_NOT_FOUND",
                    "Address not found or does not belong to the user.",
                    404
                );

            order.AddressId = orderUpdate.AddressId.Value;
        }

        db.Orders.Update(order);
        await db.SaveChangesAsync();

        return await MapToDetailDtoAsync(order);
    }

    public async Task<CreateOrderResultDto> CreateOrderFromCartAsync(string userId, OrderCreateDto orderCreateDto, string serverUrl)
    {
        // Перевіряємо адресу користувача
        var addressExists = await db.UserAddresses
            .AnyAsync(a => a.AddressId == orderCreateDto.UserAddressId && a.UserId == userId);

        if (!addressExists)
            throw new ApiException(
                "ADDRESS_NOT_FOUND",
                "Address not found or does not belong to the user.",
                404
            );

        // Отримуємо актуальний кошик через сервіс
        var cart = await cartService.GetCalculatedCartAsync(userId);

        if (cart.CartAdjusted || (cart.RemovedItems != null && cart.RemovedItems.Count > 0))
        {
            return new CreateOrderResultDto
            {
                ShoppingCart = cart,
                Message = "Cart has changed. Please review your items."
            };
        }
        if (cart.Items == null || cart.Items.Count == 0)
        {
            throw new ApiException(
                "CART_IS_EMPTY",
                "Cannot create order from empty cart.",
                400
            );
        }

        using var tx = await db.Database.BeginTransactionAsync();

        try
        {
            // створюємо замовлення
            var order = new Order
            {
                Number = await GenerateOrderNumberAsync(),
                UserId = userId,
                Status = OrderStatus.Pending,
                AddressId = orderCreateDto.UserAddressId,
                TotalOriginalPrice = cart.TotalOriginalPrice,
                TotalProductDiscount = cart.TotalProductDiscount,
                TotalCartDiscount = cart.TotalCartDiscount,
                TotalPayable = cart.TotalPayable,
            };

            order.OrderItems = [.. cart.Items.Select(ci => new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = ci.Product.Id,
                Quantity = ci.Quantity,
                UnitPrice = ci.Price,
                DiscountedUnitPrice = ci.PriceAfterCartPromotion,
            })];

            await db.Orders.AddAsync(order);
            await db.SaveChangesAsync();

            // резервування товарів
            foreach (var item in order.OrderItems)
            {
                await inventoryService.ReserveAsync(item.ProductId, order.Id, item.Quantity, PayloadValidity);
            }
            // Створюємо платіж
            var payment = await paymentService.CreatePaymentAsync(order, PayloadValidity, serverUrl);

            order.PaymentId = payment.PaymentId;

            await db.SaveChangesAsync();

            await tx.CommitAsync();
            // Запускаємо фонову задачу для перевірки платежу після закінчення терміну
            _ = Task.Run(async () =>
            {
                await Task.Delay(PayloadValidity + TimeSpan.FromMinutes(5));
                await CheckExpiredPaymentAsync(payment.PaymentId);
            });

            return new CreateOrderResultDto
            {
                Payload = payment.Payload,
                OrderId = order.Id
            };
        }
        catch
        {
            await tx.RollbackAsync();
            cart = await cartService.GetCalculatedCartAsync(userId);
            return new CreateOrderResultDto
            {
                ShoppingCart = cart,
                Message = "Order creation has failed"
            };
        }
    }

    public async Task NotifyPaymentUpdated(Guid paymentId)
    {
        PaymentResponseDto paymentResponseDto = await paymentService.GetPaymentByIdAsync(paymentId) ?? throw new ApiException(
                "PAYMENT_NOT_FOUND",
                $"Payment with Id '{paymentId}' not found.",
                404
            );
        Order order = await db.Orders
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == paymentResponseDto.OrderId)
            ?? throw new ApiException(
                "ORDER_NOT_FOUND",
                $"Order with Id '{paymentResponseDto.OrderId}' not found.",
                404
            );
        switch (paymentResponseDto.Status)
        {
            case PaymentStatus.Pending:
                break;

            case PaymentStatus.Success:
                if (order.Status == OrderStatus.Pending)
                {
                    using var tx = await db.Database.BeginTransactionAsync();
                    try
                    {
                        // Змінюємо статус замовлення
                        order.Status = OrderStatus.Processing;
                        order.UpdatedAt = DateTimeOffset.UtcNow;

                        // Підтверджуємо резервування (списуємо зі складу)
                        foreach (var item in order.OrderItems)
                        {
                            await inventoryService.ConfirmReservationAsync(item.ProductId, order.Id);
                        }

                        // Очищаємо кошик користувача
                        await cartService.ClearCartAsync(order.UserId);

                        await db.SaveChangesAsync();
                        await tx.CommitAsync();
                    }
                    catch
                    {
                        await tx.RollbackAsync();
                        throw;
                    }
                }
                else if (order.Status == OrderStatus.PaymentFailed)
                {
                    // Платіж успішний, але статус замовлення PaymentFailed
                    // Потрібно повідомити адміністратора про необхідність повернення коштів
                    // TODO: Додати логування або відправку повідомлення адміністратору!!!!!!!
                    throw new ApiException(
                        "PAYMENT_ORDER_MISMATCH",
                        "Payment succeeded but order is in PaymentFailed status. Manual intervention required.",
                        500
                    );
                }
                break;

            case PaymentStatus.Failure:
                if (order.Status == OrderStatus.Pending)
                {
                    using var tx = await db.Database.BeginTransactionAsync();
                    try
                    {
                        // Змінюємо статус замовлення
                        order.Status = OrderStatus.PaymentFailed;

                        // Скасовуємо резервування
                        foreach (var item in order.OrderItems)
                        {
                            await inventoryService.ReleaseAsync(item.ProductId, order.Id);
                        }

                        await db.SaveChangesAsync();
                        await tx.CommitAsync();
                    }
                    catch
                    {
                        await tx.RollbackAsync();
                        throw;
                    }
                }
                break;

            case PaymentStatus.Refunded:
                // Обробка повернення коштів
                if (order.Status == OrderStatus.Processing || order.Status == OrderStatus.Completed)
                {
                    order.Status = OrderStatus.Cancelled;
                    await db.SaveChangesAsync();
                }
                break;
        }
    }

    private async Task<OrderDetailDto> MapToDetailDtoAsync(Order order)
    {
        // Загружаем все product DTO одним запросом через сервис продуктов
        var productIds = order.OrderItems?.Select(oi => oi.ProductId).Distinct().ToList() ?? [];
        var products = productIds.Count > 0
            ? await productService.BuildFullDtosAsync(productIds)
            : [];
        var productDict = products.ToDictionary(p => p.Id);
        //
        return new OrderDetailDto
        {
            Id = order.Id,
            UserId = order.UserId,
            Number = order.Number,
            Status = order.Status,
            TotalOriginalPrice = order.TotalOriginalPrice,
            TotalProductDiscount = order.TotalProductDiscount,
            TotalCartDiscount = order.TotalCartDiscount,
            TotalPayable = order.TotalPayable,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Address = addressService.MapToDto(order.Address),

            Delivery = order.Delivery == null ? null : new DeliveryResponseDto
            {
                Id = order.Delivery.Id,
                OrderId = order.Id,
                TrackingNumber = order.Delivery.TrackingNumber,
                EstimatedDeliveryDate = order.Delivery.EstimatedDeliveryDate,
                ActualDeliveryDate = order.Delivery.ActualDeliveryDate,
                Status = order.Delivery.Status
            },
            Payment = order.Payment == null ? null : new PaymentResponseDto
            {
                Id = order.Payment.Id,
                OrderId = order.Id,
                Amount = order.Payment.Amount,
                Currency = order.Payment.Currency,
                LiqPayPaymentId = order.Payment.LiqPayPaymentId,
                Status = order.Payment.Status,
                CreatedAt = order.Payment.CreatedAt,
                UpdatedAt = order.Payment.UpdatedAt
            },
            OrderItems = order.OrderItems?.Select(oi =>
            {
                var prodDto = productDict[oi.ProductId];
                return new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    OrderId = oi.OrderId,
                    ProductName = prodDto.Name, // без null
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    DiscountedUnitPrice = oi.DiscountedUnitPrice,
                };
            }).ToList() ?? []
        };
    }

    public async Task NotifyDeliveryUpdated(Guid orderId, DeliveryStatus deliveryStatus)
    {
        var order = await db.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new ApiException(
                "ORDER_NOT_FOUND",
                $"Order with Id '{orderId}' not found.",
                404
            );

        // Логіка оновлення статусу замовлення в залежності від статусу доставки
        switch (deliveryStatus)
        {
            case DeliveryStatus.Pending:
                break;

            case DeliveryStatus.Shipped:
                if (order.Status == OrderStatus.Processing)
                {
                    order.Status = OrderStatus.Shipped;
                }
                break;

            case DeliveryStatus.Delivered:
                if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Processing)
                {
                    order.Status = OrderStatus.Completed;
                }
                break;

            case DeliveryStatus.Canceled:
                // Можливо, потрібно якось обробити невдалу доставку!!!
                break;
        }

        await db.SaveChangesAsync();
    }

    private async Task<string> GenerateOrderNumberAsync()
    {
        var date = DateTimeOffset.UtcNow;
        var prefix = $"ORD-{date:yyyyMMdd}";

        var lastOrder = await db.Orders
            .Where(o => o.Number.StartsWith(prefix))
            .OrderByDescending(o => o.Number)
            .Select(o => o.Number)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastOrder != null)
        {
            var lastNumberStr = lastOrder.Split('-').Last();
            if (int.TryParse(lastNumberStr, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}-{nextNumber:D4}";
    }

    private async Task CheckExpiredPaymentAsync(Guid paymentId)
    {
        try
        {
            var payment = await paymentService.GetPaymentByIdAsync(paymentId);
            if (payment != null && payment.Status == PaymentStatus.Pending)
            {
                await NotifyPaymentUpdated(paymentId);
            }
        }
        catch (Exception ex)
        {
            // TODO: Додати логування
            Console.WriteLine($"Error checking expired payment {paymentId}: {ex.Message}");
        }
    }

    private async Task RefreshPaymentStatusIfNeededAsync(Order order)
    {
        if (!order.PaymentId.HasValue || order.Payment?.Status != PaymentStatus.Pending)
        {
            return; // нічого не треба оновлювати
        }

        var freshPayment = await paymentService.GetPaymentByIdAsync(order.PaymentId.Value);
        if (freshPayment == null || freshPayment.Status == PaymentStatus.Pending)
        {
            return; // статус не змінився або платіж не знайдено
        }

        // Оновлюємо статус платежу в базі
        order.Payment.Status = freshPayment.Status;
        await db.SaveChangesAsync();

        // Якщо платіж успішний і замовлення ще в Pending — переводимо в Processing
        if (freshPayment.Status == PaymentStatus.Success && order.Status == OrderStatus.Pending)
        {
            order.Status = OrderStatus.Processing;
            await db.SaveChangesAsync();
        }
    }
}