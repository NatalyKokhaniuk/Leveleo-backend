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
using LeveLEO.Infrastructure.Events;
using LeveLEO.Infrastructure.Events.DomainEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace LeveLEO.Features.Orders.Services;

public class OrderService(
    AppDbContext db,
    IShoppingCartService cartService,
    IInventoryService inventoryService,
    IPaymentService paymentService,
    IProductService productService,
    IAddressService addressService,
    IEventBus eventBus,
    ILogger<OrderService> logger) : IOrderService
{
    /// <summary>Термін дії LiqPay payload, резерву на складі та очікування оплати перед видаленням платежу.</summary>
    private static readonly TimeSpan PayloadValidity = TimeSpan.FromMinutes(5);

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

        await RefreshPaymentStatusIfNeededAsync(order);

        order = await db.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.Address)
            .Include(o => o.Payment)
            .Include(o => o.Delivery)
            .FirstAsync(o => o.Id == orderId);

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

        await RefreshPaymentStatusIfNeededAsync(order);

        order = await db.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.Address)
            .Include(o => o.Payment)
            .Include(o => o.Delivery)
            .FirstAsync(o => o.Number == orderNumber);

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
            .Include(o => o.Address)
            .Include(o => o.Payment)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        foreach (var order in orders)
        {
            await RefreshPaymentStatusIfNeededAsync(order);
        }

        if (orders.Count > 0)
        {
            var ids = orders.Select(o => o.Id).ToList();
            var statusById = await db.Orders
                .AsNoTracking()
                .Where(o => ids.Contains(o.Id))
                .ToDictionaryAsync(o => o.Id, o => o.Status);

            foreach (var order in orders)
            {
                if (statusById.TryGetValue(order.Id, out var st))
                    order.Status = st;
            }
        }

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

    public async Task<OrderDetailDto> UpdateAsync(Guid orderId, OrderUpdateDto orderUpdate, string requestingUserId, bool isStaff)
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

        if (!isStaff)
        {
            if (order.UserId != requestingUserId)
                throw new ApiException(
                    "FORBIDDEN",
                    "You can only view or edit your own orders.",
                    403
                );
            if (order.Status != OrderStatus.Pending)
                throw new ApiException(
                    "ORDER_NOT_EDITABLE",
                    "Only pending orders can be updated by the customer.",
                    400
                );
        }

        if (isStaff && orderUpdate.Status.HasValue)
            order.Status = orderUpdate.Status.Value;

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
        var addressExists = await db.UserAddresses
            .AnyAsync(a => a.AddressId == orderCreateDto.UserAddressId && a.UserId == userId);

        if (!addressExists)
            throw new ApiException(
                "ADDRESS_NOT_FOUND",
                "Address not found or does not belong to the user.",
                404
            );

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

        var strategy = db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            Order? order = null;
            CreatePaymentResultDto? payment = null;

            await using var tx = await db.Database.BeginTransactionAsync();
            var committed = false;

            try
            {
                order = new Order
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
                payment = await paymentService.CreatePaymentAsync(order, PayloadValidity, serverUrl);

                order.PaymentId = payment.PaymentId;

                await db.SaveChangesAsync();

                await tx.CommitAsync();
                committed = true;
            }
            catch
            {
                if (!committed)
                    await tx.RollbackAsync();
                cart = await cartService.GetCalculatedCartAsync(userId);
                return new CreateOrderResultDto
                {
                    ShoppingCart = cart,
                    Message = "Order creation has failed"
                };
            }

            var userEmail = await db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            await eventBus.PublishAsync(new OrderCreatedEvent
            {
                OrderId = order!.Id,
                OrderNumber = order.Number,
                UserId = order.UserId,
                UserEmail = userEmail!,
                TotalPayable = order.TotalPayable
            });

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(PayloadValidity);
                    await TryAbandonExpiredUnpaidPaymentAsync(payment!.PaymentId);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Delayed expiry cleanup failed for payment {PaymentId}", payment!.PaymentId);
                }
            });

            return new CreateOrderResultDto
            {
                OrderId = order.Id,
                Data = payment!.Payload,
                Payload = payment.Payload,
                Signature = payment.Signature
            };
        });
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
            .Include(o => o.OrderItems)
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
                    var strategy = db.Database.CreateExecutionStrategy();
                    await strategy.ExecuteAsync(async () =>
                    {
                        await using var tx = await db.Database.BeginTransactionAsync();
                        var committed = false;
                        try
                        {
                            order.Status = OrderStatus.Processing;
                            order.UpdatedAt = DateTimeOffset.UtcNow;

                            foreach (var item in order.OrderItems)
                            {
                                await inventoryService.ConfirmReservationAsync(item.ProductId, order.Id);
                            }

                            await cartService.ClearCartAsync(order.UserId);

                            await db.SaveChangesAsync();
                            await tx.CommitAsync();
                            committed = true;
                        }
                        catch
                        {
                            if (!committed)
                                await tx.RollbackAsync();
                            throw;
                        }

                        var userEmail = await db.Users.AsNoTracking()
                            .Where(u => u.Id == order.UserId)
                            .Select(u => u.Email)
                            .FirstOrDefaultAsync();

                        await eventBus.PublishAsync(new OrderPaidEvent
                        {
                            OrderId = order.Id,
                            OrderNumber = order.Number,
                            UserId = order.UserId,
                            UserEmail = userEmail!,
                            AmountPaid = paymentResponseDto.Amount
                        });
                    });
                }
                else if (order.Status == OrderStatus.PaymentFailed)
                {
                    // Платіж успішний, але статус замовлення PaymentFailed
                    // Потрібно повідомити адміністратора про необхідність повернення коштів
                    await eventBus.PublishAsync(new PaymentOrderMismatchEvent
                    {
                        OrderId = order.Id,
                        PaymentId = paymentId,
                        OrderNumber = order.Number,
                        UserId = order.UserId,
                        Reason = "Payment succeeded but order is in PaymentFailed status. Manual intervention required."
                    });

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
                    var strategy = db.Database.CreateExecutionStrategy();
                    await strategy.ExecuteAsync(async () =>
                    {
                        await using var tx = await db.Database.BeginTransactionAsync();
                        var committed = false;
                        try
                        {
                            order.Status = OrderStatus.Cancelled;
                            order.UpdatedAt = DateTimeOffset.UtcNow;

                            foreach (var item in order.OrderItems)
                            {
                                await inventoryService.ReleaseAsync(item.ProductId, order.Id);
                            }

                            await db.SaveChangesAsync();
                            await tx.CommitAsync();
                            committed = true;
                        }
                        catch
                        {
                            if (!committed)
                                await tx.RollbackAsync();
                            throw;
                        }
                    });
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
        var productIds = order.OrderItems?.Select(oi => oi.ProductId).Distinct().ToList() ?? [];
        var products = productIds.Count > 0
            ? await productService.BuildFullDtosAsync(productIds)
            : [];
        var productDict = products.ToDictionary(p => p.Id);
        
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
                    ProductName = prodDto.Name,
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
            .Include(o => o.User)
            .Include(o => o.Delivery)
            .Include(o => o.OrderItems)
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
                    await db.SaveChangesAsync();
                    
                    await eventBus.PublishAsync(new OrderShippedEvent
                    {
                        OrderId = order.Id,
                        OrderNumber = order.Number,
                        UserId = order.UserId,
                        UserEmail = order.User.Email!,
                        TrackingNumber = order.Delivery?.TrackingNumber
                    });
                }
                break;

            case DeliveryStatus.Delivered:
                if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Processing)
                {
                    order.Status = OrderStatus.Completed;
                    await db.SaveChangesAsync();

                    await eventBus.PublishAsync(new OrderCompletedEvent
                    {
                        OrderId = order.Id,
                        OrderNumber = order.Number,
                        UserId = order.UserId,
                        UserEmail = order.User.Email!,
                        ProductIds = [.. order.OrderItems.Select(oi => oi.ProductId)]
                    });
                }
                break;

            case DeliveryStatus.Canceled:
                // TODO: Додати обробку скасованої доставки
                break;
        }
    }

    public async Task<PagedResultDto<OrderListItemDto>> GetAllOrdersAsync(AdminOrderFilterDto filter)
    {
        var query = db.Orders
            .Include(o => o.Address)
            .AsQueryable();

        // Фільтрація по статусу
        if (filter.Status.HasValue)
        {
            query = query.Where(o => o.Status == filter.Status.Value);
        }

        // Фільтрація по датах
        if (filter.StartDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= filter.EndDate.Value);
        }

        // Сортування
        query = filter.SortBy?.ToLower() switch
        {
            "totalpayable" => filter.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(o => o.TotalPayable)
                : query.OrderByDescending(o => o.TotalPayable),
            "status" => filter.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(o => o.Status)
                : query.OrderByDescending(o => o.Status),
            _ => filter.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(o => o.CreatedAt)
                : query.OrderByDescending(o => o.CreatedAt)
        };

        var totalCount = await query.CountAsync();

        var orders = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var items = orders.Select(o => new OrderListItemDto
        {
            Id = o.Id,
            Number = o.Number,
            CreatedAt = o.CreatedAt,
            Status = o.Status,
            TotalPayable = o.TotalPayable,
            AddressSummary = $"{o.Address?.CityName}, {o.Address?.Street} {o.Address?.House}"
        }).ToList();

        return new PagedResultDto<OrderListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
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

    public async Task ProcessExpiredPendingPaymentsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var ids = await db.Payments
            .AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Pending && p.ExpireAt <= now)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        foreach (var id in ids)
            await TryAbandonExpiredUnpaidPaymentAsync(id);
    }

    /// <summary>
    /// Якщо термін оплати минув і платіж досі Pending — знімаємо резерви, скасовуємо замовлення, видаляємо запис платежу.
    /// </summary>
    private async Task TryAbandonExpiredUnpaidPaymentAsync(Guid paymentId)
    {
        try
        {
            var strategy = db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync();
                var committed = false;
                try
                {
                    var paymentEntity = await db.Payments
                        .Include(p => p.Order)
                        .ThenInclude(o => o.OrderItems)
                        .FirstOrDefaultAsync(p => p.Id == paymentId);

                    if (paymentEntity == null || paymentEntity.Status != PaymentStatus.Pending)
                    {
                        await tx.RollbackAsync();
                        return;
                    }

                    if (paymentEntity.ExpireAt > DateTimeOffset.UtcNow)
                    {
                        await tx.RollbackAsync();
                        return;
                    }

                    var order = paymentEntity.Order;
                    if (order.Status != OrderStatus.Pending)
                    {
                        await tx.RollbackAsync();
                        return;
                    }

                    order.PaymentId = null;
                    order.Status = OrderStatus.Cancelled;
                    order.UpdatedAt = DateTimeOffset.UtcNow;

                    foreach (var item in order.OrderItems)
                        await inventoryService.ReleaseAsync(item.ProductId, order.Id);

                    db.Payments.Remove(paymentEntity);
                    await db.SaveChangesAsync();
                    await tx.CommitAsync();
                    committed = true;
                }
                catch
                {
                    if (!committed)
                        await tx.RollbackAsync();
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to remove expired unpaid payment {PaymentId}", paymentId);
        }
    }

    private async Task RefreshPaymentStatusIfNeededAsync(Order order)
    {
        if (!order.PaymentId.HasValue)
            return;

        if (order.Payment is not { Status: PaymentStatus.Pending })
            return;

        if (order.Payment.ExpireAt <= DateTimeOffset.UtcNow)
        {
            await TryAbandonExpiredUnpaidPaymentAsync(order.PaymentId.Value);
            return;
        }

        await paymentService.GetPaymentByIdAsync(order.PaymentId.Value);
    }
}
