using LeveLEO.Infrastructure.Delivery.DTO;

namespace LeveLEO.Infrastructure.Delivery;

public interface INovaPoshtaService
{
    // Пошук міст
    Task<List<CityDto>> SearchCitiesAsync(string query, int limit = 10);

    // Отримання відділень міста
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