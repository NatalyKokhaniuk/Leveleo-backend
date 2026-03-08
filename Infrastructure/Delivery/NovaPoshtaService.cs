using LeveLEO.Infrastructure.Delivery.DTO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LeveLEO.Infrastructure.Delivery;

public class NovaPoshtaService(
    HttpClient httpClient,
    IConfiguration config,
    ILogger<NovaPoshtaService> logger) : INovaPoshtaService
{
    private readonly string _apiKey = config["NovaPoshta:ApiKey"]
            ?? throw new InvalidOperationException("NovaPoshta API key not configured");

    private const string ApiUrl = "https://api.novaposhta.ua/v2.0/json/";

    #region Довідники (Cities, Warehouses, Streets)

    public async Task<List<CityDto>> SearchCitiesAsync(string query, int limit = 10)
    {
        var request = new NovaPoshtaRequest
        {
            ApiKey = _apiKey,
            ModelName = "Address",
            CalledMethod = "searchSettlements",
            MethodProperties = new
            {
                CityName = query,
                Limit = limit
            }
        };

        var response = await SendRequestAsync<NovaPoshtaResponse<List<CitySearchResult>>>(request);

        if (!response.Success || response.Data == null || response.Data.Count == 0)
        {
            return [];
        }

        var cities = response.Data[0].Addresses ?? [];
        return [.. cities.Take(limit)];
    }

    public async Task<List<WarehouseDto>> GetWarehousesByCityAsync(string cityRef, int page = 1, int limit = 50)
    {
        var request = new NovaPoshtaRequest
        {
            ApiKey = _apiKey,
            ModelName = "Address",
            CalledMethod = "getWarehouses",
            MethodProperties = new
            {
                CityRef = cityRef,
                Page = page,
                Limit = limit
            }
        };

        var response = await SendRequestAsync<NovaPoshtaResponse<List<WarehouseDto>>>(request);

        return response.Success && response.Data != null ? response.Data : [];
    }

    public async Task<List<StreetDto>> SearchStreetsAsync(string cityRef, string query, int limit = 10)
    {
        var request = new NovaPoshtaRequest
        {
            ApiKey = _apiKey,
            ModelName = "Address",
            CalledMethod = "searchSettlementStreets",
            MethodProperties = new
            {
                StreetName = query,
                SettlementRef = cityRef,
                Limit = limit
            }
        };

        var response = await SendRequestAsync<NovaPoshtaResponse<List<CityStreetSearchResult>>>(request);

        if (!response.Success || response.Data == null || response.Data.Count == 0)
        {
            return [];
        }

        var streets = response.Data[0].Addresses ?? [];
        return [.. streets.Take(limit)];
    }

    #endregion Довідники (Cities, Warehouses, Streets)

    #region Експрес-накладні (ЕН)

    public async Task<InternetDocumentDto> CreateInternetDocumentAsync(CreateInternetDocumentDto dto)
    {
        var request = new NovaPoshtaRequest
        {
            ApiKey = _apiKey,
            ModelName = "InternetDocument",
            CalledMethod = "save",
            MethodProperties = dto
        };

        var response = await SendRequestAsync<NovaPoshtaResponse<List<InternetDocumentDto>>>(request);

        if (!response.Success || response.Data == null || response.Data.Count == 0)
        {
            var errors = string.Join(", ", response.Errors ?? []);
            throw new ApiException(
                "NOVA_POSHTA_ERROR",
                $"Failed to create internet document: {errors}",
                400
            );
        }

        return response.Data[0];
    }

    public async Task<InternetDocumentDto> GetInternetDocumentAsync(string documentRef)
    {
        var request = new NovaPoshtaRequest
        {
            ApiKey = _apiKey,
            ModelName = "InternetDocument",
            CalledMethod = "getDocumentList",
            MethodProperties = new
            {
                Ref = documentRef
            }
        };

        var response = await SendRequestAsync<NovaPoshtaResponse<List<InternetDocumentDto>>>(request);

        if (!response.Success || response.Data == null || response.Data.Count == 0)
        {
            throw new ApiException("DOCUMENT_NOT_FOUND", "Internet document not found", 404);
        }

        return response.Data[0];
    }

    public async Task<List<TrackingEventDto>> TrackParcelAsync(string trackingNumber)
    {
        var request = new NovaPoshtaRequest
        {
            ApiKey = _apiKey,
            ModelName = "TrackingDocument",
            CalledMethod = "getStatusDocuments",
            MethodProperties = new
            {
                Documents = new[]
                {
                    new { DocumentNumber = trackingNumber }
                }
            }
        };

        var response = await SendRequestAsync<NovaPoshtaResponse<List<TrackingEventDto>>>(request);

        return response.Success && response.Data != null ? response.Data : [];
    }

    public async Task<bool> DeleteInternetDocumentAsync(string documentRef)
    {
        var request = new NovaPoshtaRequest
        {
            ApiKey = _apiKey,
            ModelName = "InternetDocument",
            CalledMethod = "delete",
            MethodProperties = new
            {
                DocumentRefs = documentRef
            }
        };

        var response = await SendRequestAsync<NovaPoshtaResponse<List<DeleteDocumentResult>>>(request);

        return response.Success && response.Data != null && response.Data.Count > 0 && response.Data[0].Ref == documentRef;
    }

    #endregion Експрес-накладні (ЕН)

    #region Helper Methods

    private async Task<T> SendRequestAsync<T>(NovaPoshtaRequest request)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(ApiUrl, request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<T>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? throw new InvalidOperationException("Empty response from Nova Poshta API");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Nova Poshta API request failed");
            throw new ApiException("NOVA_POSHTA_ERROR", "Failed to communicate with Nova Poshta API", 500);
        }
    }

    #endregion Helper Methods
}