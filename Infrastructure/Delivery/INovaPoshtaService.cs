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

    /// <summary>Усі поштомати обраного населеного пункту (за Ref з довідника / searchSettlements).</summary>
    Task<List<WarehouseDto>> GetPostomatsBySettlementAsync(string settlementRef);

    /// <summary>Усі відділення (не поштомати) з адресами для населеного пункту.</summary>
    Task<List<WarehouseDto>> GetBranchWarehousesBySettlementAsync(string settlementRef);

    // Отримання відділень міста (сторінка)
    Task<List<WarehouseDto>> GetWarehousesByCityAsync(string cityRef, int page = 1, int limit = 50);

    // Пошук вулиць
    Task<List<StreetDto>> SearchStreetsAsync(string cityRef, string query, int limit = 10);

    // Створення ЕН (експрес-накладна)
    Task<InternetDocumentDto> CreateInternetDocumentAsync(CreateInternetDocumentDto dto);

    // Отримання інформації про ЕН
    Task<InternetDocumentDto> GetInternetDocumentAsync(string documentRef);

    // Відстеження посилки
    Task<List<TrackingEventDto>> TrackParcelAsync(string trackingNumber);

    // Видалення ЕН
    Task<bool> DeleteInternetDocumentAsync(string documentRef);
}