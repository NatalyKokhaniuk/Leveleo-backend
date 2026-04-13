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

    private const int SettlementsPageSize = 150;
    private const int WarehouseFetchLimit = 500;
    private const int MaxWarehousePages = 100;

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

    public async Task<SettlementsPageDto> GetSettlementsPageAsync(int page, string? findByString = null)
    {
        if (page < 1)
        {
            page = 1;
        }

        object methodProps = string.IsNullOrWhiteSpace(findByString)
            ? new { Page = page }
            : new { Page = page, FindByString = findByString.Trim() };

        var request = new NovaPoshtaRequest
        {
            ApiKey = _apiKey,
            ModelName = "AddressGeneral",
            CalledMethod = "getSettlements",
            MethodProperties = methodProps
        };

        var response = await SendRequestAsync<NovaPoshtaResponse<List<SettlementDirectoryDto>>>(request);

        if (!response.Success || response.Data == null)
        {
            logger.LogWarning(
                "Nova Poshta getSettlements failed (page {Page}): {Errors}",
                page,
                string.Join(", ", response.Errors ?? []));
            return new SettlementsPageDto
            {
                Page = page,
                PageSize = 0,
                Items = [],
                HasMore = false
            };
        }

        var items = response.Data;
        return new SettlementsPageDto
        {
            Page = page,
            PageSize = items.Count,
            Items = items,
            HasMore = items.Count == SettlementsPageSize
        };
    }

    public async Task<List<WarehouseDto>> GetPostomatsBySettlementAsync(string settlementRef)
    {
        var all = await FetchAllWarehousesForSettlementAsync(settlementRef);
        return [.. all.Where(IsPostomat)];
    }

    public async Task<List<WarehouseDto>> GetBranchWarehousesBySettlementAsync(string settlementRef)
    {
        var all = await FetchAllWarehousesForSettlementAsync(settlementRef);
        return [.. all.Where(w => !IsPostomat(w))];
    }

    private async Task<List<WarehouseDto>> FetchAllWarehousesForSettlementAsync(string settlementRef)
    {
        var all = new List<WarehouseDto>();
        for (var page = 1; page <= MaxWarehousePages; page++)
        {
            var batch = await GetWarehousesByCityAsync(settlementRef, page, WarehouseFetchLimit);
            if (batch.Count == 0)
            {
                break;
            }

            all.AddRange(batch);
            if (batch.Count < WarehouseFetchLimit)
            {
                break;
            }
        }

        return all;
    }

    private static bool IsPostomat(WarehouseDto w)
    {
        if (!string.IsNullOrEmpty(w.TypeOfWarehouse) &&
            w.TypeOfWarehouse.Contains("Поштомат", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var d = w.Description ?? string.Empty;
        return d.Contains("Поштомат", StringComparison.OrdinalIgnoreCase)
            || d.Contains("Почтомат", StringComparison.OrdinalIgnoreCase);
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