using LeveLEO.Infrastructure.Delivery.DTO;

namespace LeveLEO.Infrastructure.Delivery;

public interface INovaPoshtaService
{
    // Пошук міст
    Task<List<CityDto>> SearchCitiesAsync(string query, int limit = 10);

    /// <summary>
    /// Довідник населених пунктів НП (getSettlements), сторінка за номером. Рекомендовано кешувати на клієнті.
    /// </summary>
    Task<SettlementsPageDto> GetSettlementsPageAsync(int page, string? findByString = null);

    /// <summary>Поштомати. У параметр <paramref name="deliveryCityRef"/> передавайте <c>deliveryCity</c> з cities/search. Опційний <c>FindByString</c> НП.</summary>
    Task<List<WarehouseDto>> GetPostomatsBySettlementAsync(string deliveryCityRef, string? findByString = null);

    /// <summary>Відділення (категорія Branch). У параметр передавайте <c>deliveryCity</c> з cities/search. Опційний <c>FindByString</c> НП.</summary>
    Task<List<WarehouseDto>> GetBranchWarehousesBySettlementAsync(string deliveryCityRef, string? findByString = null);

    /// <summary>Довідник типів складів (<c>getWarehouseTypes</c>); результат кешується на бекенді.</summary>
    Task<List<WarehouseTypeDto>> GetWarehouseTypesAsync(bool forceRefresh = false);

    // Отримання відділень міста (сторінка)
    Task<List<WarehouseDto>> GetWarehousesByCityAsync(string cityRef, int page = 1, int limit = 50);

    /// <summary>Знайти відділення/поштомат за Ref у межах міста (<c>deliveryCity</c> із cities/search). Постранінковий пошук у НП.</summary>
    Task<WarehouseDto?> GetWarehouseByCityAndRefAsync(string deliveryCityRef, string warehouseRef);

    /// <summary>Пошук вулиць (searchSettlementStreets). <paramref name="settlementRef"/> — Ref населеного пункту з міста.</summary>
    Task<List<StreetDto>> SearchStreetsAsync(string settlementRef, string streetQuery, int limit = 20);

    // Створення ЕН (експрес-накладна)
    Task<InternetDocumentDto> CreateInternetDocumentAsync(CreateInternetDocumentDto dto);

    // Отримання інформації про ЕН
    Task<InternetDocumentDto> GetInternetDocumentAsync(string documentRef);

    // Відстеження посилки
    Task<List<TrackingEventDto>> TrackParcelAsync(string trackingNumber);

    // Видалення ЕН
    Task<bool> DeleteInternetDocumentAsync(string documentRef);
}