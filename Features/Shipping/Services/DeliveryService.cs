// Features/Shipping/Services/DeliveryService.cs
using LeveLEO.Data;
using LeveLEO.Features.Orders.Models;
using LeveLEO.Features.Orders.Services;
using LeveLEO.Features.Shipping.DTO;
using LeveLEO.Features.Shipping.Models;
using LeveLEO.Infrastructure.Delivery;
using LeveLEO.Infrastructure.Delivery.DTO;
using Microsoft.EntityFrameworkCore;

namespace LeveLEO.Features.Shipping.Services;

public class DeliveryService(
    AppDbContext db,
    INovaPoshtaService novaPoshtaService,
    IOrderService orderService,
    IConfiguration config,
    ILogger<DeliveryService> logger) : IDeliveryService
{
    public async Task<DeliveryResponseDto> CreateDeliveryAsync(Guid orderId)
    {
        var order = await db.Orders
            .Include(o => o.Address)
            .Include(o => o.OrderItems)
            .Include(o => o.Delivery)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new ApiException("ORDER_NOT_FOUND", "Order not found.", 404);

        // Перевірки
        if (order.Status != OrderStatus.Processing)
        {
            throw new ApiException(
                "INVALID_ORDER_STATUS",
                "Only processing orders can have delivery created.",
                400
            );
        }

        if (order.Delivery != null)
        {
            throw new ApiException(
                "DELIVERY_ALREADY_EXISTS",
                "Delivery already exists for this order.",
                400
            );
        }

        var address = order.Address;

        var createDocumentDto = new CreateInternetDocumentDto
        {
            DateTime = NovaPoshtaHelpers.FormatDate(DateTimeOffset.Now),

            // Відправник (з конфігурації)
            CitySender = config["NovaPoshta:SenderCityRef"]
                ?? throw new InvalidOperationException("Sender city not configured"),
            SenderName = config["NovaPoshta:SenderName"]
                ?? throw new InvalidOperationException("Sender name not configured"),
            SendersPhone = NovaPoshtaHelpers.FormatPhone(
                config["NovaPoshta:SenderPhone"]
                ?? throw new InvalidOperationException("Sender phone not configured")),

            // Отримувач (з адреси замовлення)
            RecipientCityRef = address.CityRef
                ?? throw new ApiException("INVALID_ADDRESS", "City is required.", 400),
            RecipientName = address.FirstName,
            RecipientSurname = address.LastName,
            RecipientMiddleName = address.MiddleName,
            RecipientsPhone = NovaPoshtaHelpers.FormatPhone(address.PhoneNumber),

            // Опис замовлення
            Description = $"Замовлення #{order.Number}",
            Cost = order.TotalPayable,
            Weight = 0.5m,
            SeatsAmount = order.OrderItems.Sum(oi => oi.Quantity),

            // Тип доставки
            ServiceType = DetermineServiceType(address),
            PayerType = "Sender",
            PaymentMethod = "NonCash",
            CargoType = "Cargo"
        };

        // Адреси відправника і отримувача
        if (address.DeliveryType == DeliveryType.Warehouse)
        {
            createDocumentDto.SenderWarehouse = config["NovaPoshta:SenderWarehouseRef"];
            createDocumentDto.RecipientWarehouse = address.WarehouseRef
                ?? throw new ApiException("INVALID_ADDRESS", "Warehouse is required.", 400);
        }
        else if (address.DeliveryType == DeliveryType.Doors)
        {
            createDocumentDto.SenderWarehouse = config["NovaPoshta:SenderWarehouseRef"];
            createDocumentDto.RecipientAddress = address.StreetRef;
        }

        try
        {
            var npDocument = await novaPoshtaService.CreateInternetDocumentAsync(createDocumentDto);
            var delivery = new Delivery
            {
                OrderId = orderId,
                AddressId = order.AddressId,
                TrackingNumber = npDocument.IntDocNumber,
                NovaPoshtaDocumentRef = npDocument.Ref,
                Status = DeliveryStatus.Shipped,
                EstimatedDeliveryDate = ParseEstimatedDate(npDocument.EstimatedDeliveryDate),
                DeliveryCost = npDocument.CostOnSite,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            db.Deliveries.Add(delivery);
            order.DeliveryId = delivery.Id;

            await db.SaveChangesAsync();

            await orderService.NotifyDeliveryUpdated(orderId, DeliveryStatus.Shipped);

            logger.LogInformation(
                "Delivery created for order {OrderId}, tracking: {TrackingNumber}",
                orderId, delivery.TrackingNumber
            );

            return await MapToDtoAsync(delivery);
        }
        catch (ApiException ex) when (ex.ErrorCode == "NOVA_POSHTA_ERROR")
        {
            logger.LogError(ex, "Failed to create NP document for order {OrderId}", orderId);
            throw new ApiException(
                "DELIVERY_CREATION_FAILED",
                "Failed to create delivery. Please try again later.",
                500
            );
        }
    }

    public async Task<DeliveryResponseDto> GetDeliveryByIdAsync(Guid deliveryId)
    {
        var delivery = await db.Deliveries
            .Include(d => d.Address)
            .Include(d => d.Order)
            .FirstOrDefaultAsync(d => d.Id == deliveryId)
            ?? throw new ApiException("DELIVERY_NOT_FOUND", "Delivery not found.", 404);

        return await MapToDtoAsync(delivery);
    }

    public async Task<DeliveryResponseDto> GetDeliveryByOrderIdAsync(Guid orderId)
    {
        var delivery = await db.Deliveries
            .Include(d => d.Address)
            .Include(d => d.Order)
            .FirstOrDefaultAsync(d => d.OrderId == orderId)
            ?? throw new ApiException("DELIVERY_NOT_FOUND", "Delivery not found for this order.", 404);

        return await MapToDtoAsync(delivery);
    }

    public async Task<DeliveryResponseDto> GetDeliveryByOrderNumberAsync(string orderNumber)
    {
        var delivery = await db.Deliveries
            .Include(d => d.Address)
            .Include(d => d.Order)
            .FirstOrDefaultAsync(d => d.Order.Number == orderNumber)
            ?? throw new ApiException(
                "DELIVERY_NOT_FOUND",
                $"Delivery not found for order {orderNumber}.",
                404
            );

        return await MapToDtoAsync(delivery);
    }

    public async Task<DeliveryResponseDto> GetDeliveryByTrackingNumberAsync(string trackingNumber)
    {
        var delivery = await db.Deliveries
            .Include(d => d.Address)
            .Include(d => d.Order)
            .FirstOrDefaultAsync(d => d.TrackingNumber == trackingNumber)
            ?? throw new ApiException("DELIVERY_NOT_FOUND", "Delivery not found.", 404);

        return await MapToDtoAsync(delivery);
    }

    public async Task<DeliveryResponseDto> UpdateDeliveryStatusAsync(Guid deliveryId)
    {
        var delivery = await db.Deliveries
            .Include(d => d.Address)
            .Include(d => d.Order)
            .FirstOrDefaultAsync(d => d.Id == deliveryId)
            ?? throw new ApiException("DELIVERY_NOT_FOUND", "Delivery not found.", 404);

        var trackingEvents = await novaPoshtaService.TrackParcelAsync(delivery.TrackingNumber);

        if (trackingEvents.Count > 0)
        {
            var latestEvent = trackingEvents[0];
            var oldStatus = delivery.Status;

            delivery.Status = MapNovaPoshtaStatus(latestEvent.StatusCode);

            if (delivery.Status == DeliveryStatus.Delivered && delivery.ActualDeliveryDate == null)
            {
                delivery.ActualDeliveryDate = DateTimeOffset.UtcNow;
            }

            delivery.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();

            if (oldStatus != delivery.Status)
            {
                await orderService.NotifyDeliveryUpdated(delivery.OrderId, delivery.Status);

                logger.LogInformation(
                    "Delivery {DeliveryId} status: {OldStatus} → {NewStatus}",
                    deliveryId, oldStatus, delivery.Status
                );
            }
        }

        return await MapToDtoAsync(delivery);
    }

    public async Task<bool> CancelDeliveryAsync(Guid deliveryId)
    {
        var delivery = await db.Deliveries
            .Include(d => d.Order)
            .FirstOrDefaultAsync(d => d.Id == deliveryId)
            ?? throw new ApiException("DELIVERY_NOT_FOUND", "Delivery not found.", 404);

        if (delivery.Status == DeliveryStatus.Delivered)
        {
            throw new ApiException(
                "CANNOT_CANCEL_DELIVERED",
                "Cannot cancel already delivered parcel.",
                400
            );
        }

        try
        {
            var deleted = await novaPoshtaService.DeleteInternetDocumentAsync(delivery.NovaPoshtaDocumentRef!);

            if (deleted)
            {
                delivery.Status = DeliveryStatus.Canceled;
                delivery.UpdatedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync();

                await orderService.NotifyDeliveryUpdated(delivery.OrderId, DeliveryStatus.Canceled);

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to cancel delivery {DeliveryId}", deliveryId);
            throw new ApiException("DELIVERY_CANCELLATION_FAILED", "Failed to cancel delivery.", 500);
        }
    }

    public async Task<List<TrackingEventDto>> GetTrackingHistoryAsync(Guid deliveryId)
    {
        var delivery = await db.Deliveries
            .FirstOrDefaultAsync(d => d.Id == deliveryId)
            ?? throw new ApiException("DELIVERY_NOT_FOUND", "Delivery not found.", 404);

        return await novaPoshtaService.TrackParcelAsync(delivery.TrackingNumber);
    }

    #region Helpers

    private async Task<DeliveryResponseDto> MapToDtoAsync(Delivery delivery)
    {
        var address = delivery.Address ?? await db.Addresses.FindAsync(delivery.AddressId);

        return new DeliveryResponseDto
        {
            Id = delivery.Id,
            OrderId = delivery.OrderId,
            TrackingNumber = delivery.TrackingNumber,
            Status = delivery.Status,
            EstimatedDeliveryDate = delivery.EstimatedDeliveryDate,
            ActualDeliveryDate = delivery.ActualDeliveryDate,
            CreatedAt = delivery.CreatedAt,
            UpdatedAt = delivery.UpdatedAt,
            NovaPoshtaDocumentRef = delivery.NovaPoshtaDocumentRef,
            DeliveryCost = delivery.DeliveryCost,
            Address = new AddressResponseDto
            {
                Id = address!.Id,
                FirstName = address.FirstName,
                LastName = address.LastName,
                MiddleName = address.MiddleName,
                PhoneNumber = address.PhoneNumber,
                DeliveryType = address.DeliveryType,
                FormattedAddress = FormatAddress(address),
                CityName = address.CityName,
                WarehouseDescription = address.WarehouseDescription,
                Street = address.Street,
                House = address.House,
                Flat = address.Flat,
                AdditionalInfo = address.AdditionalInfo
            }
        };
    }

    private static string FormatAddress(Address address)
    {
        return address.DeliveryType switch
        {
            DeliveryType.Warehouse => $"{address.CityName}, {address.WarehouseDescription}",
            DeliveryType.Doors => $"{address.CityName}, {address.Street} {address.House}" +
                                  (string.IsNullOrWhiteSpace(address.Flat) ? "" : $", кв. {address.Flat}"),
            _ => address.CityName ?? ""
        };
    }

    private static string DetermineServiceType(Address address)
    {
        return address.DeliveryType switch
        {
            DeliveryType.Warehouse => "WarehouseWarehouse",
            DeliveryType.Doors => "WarehouseDoors",
            _ => "WarehouseWarehouse"
        };
    }

    private static DateTimeOffset ParseEstimatedDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return DateTimeOffset.UtcNow.AddDays(2);

        if (DateTimeOffset.TryParse(dateStr, out var date))
            return date;

        return DateTimeOffset.UtcNow.AddDays(2);
    }

    private static DeliveryStatus MapNovaPoshtaStatus(string statusCode)
    {
        return statusCode switch
        {
            "1" or "2" or "3" => DeliveryStatus.Shipped,
            "4" or "41" or "5" => DeliveryStatus.InTransit,
            "6" or "7" or "8" => DeliveryStatus.OutForDelivery,
            "9" or "10" or "11" => DeliveryStatus.Delivered,
            "101" or "102" or "103" => DeliveryStatus.Canceled,
            _ => DeliveryStatus.Pending
        };
    }

    #endregion Helpers
}
