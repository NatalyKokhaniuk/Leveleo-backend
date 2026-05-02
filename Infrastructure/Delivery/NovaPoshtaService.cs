using LeveLEO.Infrastructure.Delivery.DTO;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace LeveLEO.Infrastructure.Delivery;

public class NovaPoshtaService(
    HttpClient httpClient,
    IConfiguration config,
    ILogger<NovaPoshtaService> logger) : INovaPoshtaService
{
    private readonly string _apiKey = config["NovaPoshta:ApiKey"]
            ?? throw new InvalidOperationException("NovaPoshta API key not configured");

    private readonly bool _logFullNovaPoshtaExchange = config.GetValue("NovaPoshta:LogFullExchange", false);

    private const string ApiUrl = "https://api.novaposhta.ua/v2.0/json/";

    /// <summary>Як у прикладі з кабінету НП: корінь camelCase (<c>apiKey</c>), у <c>methodProperties</c> — PascalCase (CityName тощо).</summary>
    private static readonly JsonSerializerOptions NovaPoshtaOutboundJson = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions NovaPoshtaInboundJson = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = null
    };

    private const int SettlementsPageSize = 150;
    private const int WarehouseFetchLimit = 500;
    private const int MaxWarehousePages = 100;

    private static readonly TimeSpan WarehouseTypesCacheTtl = TimeSpan.FromHours(24);
    private static readonly SemaphoreSlim WarehouseTypesLock = new(1, 1);

    private static WarehouseTypesSnapshot? _warehouseTypesCache;
    private static DateTimeOffset _warehouseTypesFetchedUtc;

    #region Довідники (Cities, Warehouses, Streets)

    public async Task<List<CityDto>> SearchCitiesAsync(string query, int limit = 10)
    {
        var limitClamped = limit > 0 ? limit : 20;

        // Офіційний формат: Limit/Page як рядки, корінь camelCase.
        var methodProps = new JsonObject
        {
            ["CityName"] = query,
            ["Limit"] = limitClamped.ToString(CultureInfo.InvariantCulture),
            ["Page"] = "1"
        };

        var envelope = new JsonObject
        {
            ["apiKey"] = _apiKey,
            ["modelName"] = "AddressGeneral",
            ["calledMethod"] = "searchSettlements",
            ["methodProperties"] = methodProps
        };

        var jsonBody = envelope.ToJsonString(NovaPoshtaOutboundJson);

        var response = await PostNovaPoshtaJsonAsync<NovaPoshtaResponse<List<CitySearchResult>>>(
            jsonBody,
            "AddressGeneral.searchSettlements");

        if (response.Success
            && response.Data is { Count: > 0 }
            && response.Data[0].Addresses is { Count: > 0 } addresses)
        {
            return [.. addresses.Take(limitClamped)];
        }

        if (!response.Success)
        {
            logger.LogWarning(
                "Nova Poshta searchSettlements failed for query {Query}: {Errors}",
                query,
                response.Errors is { Count: > 0 } errList ? string.Join("; ", errList) : "(no errors)");
        }
        else
        {
            logger.LogInformation(
                "Nova Poshta searchSettlements returned no addresses for query {Query}; using getSettlements fallback.",
                query);
        }

        var page = await GetSettlementsPageAsync(1, query);
        return page.Items.Count == 0
            ? []
            : [.. page.Items.Select(ToCityDtoFromSettlement).Take(limitClamped)];
    }

    private static CityDto ToCityDtoFromSettlement(SettlementDirectoryDto s)
    {
        var warehouses = s.Warehouses
            ?? (int.TryParse(s.Warehouse, NumberStyles.Integer, CultureInfo.InvariantCulture, out var wh)
                ? wh
                : 0);

        return new CityDto
        {
            Ref = s.Ref,
            DeliveryCity = string.IsNullOrWhiteSpace(s.DeliveryCity) ? null : s.DeliveryCity.Trim(),
            Present = s.Description,
            MainDescription = s.Description,
            Area = s.Area ?? string.Empty,
            Region = s.RegionsDescription ?? s.Region ?? string.Empty,
            SettlementTypeCode = s.SettlementType ?? string.Empty,
            Warehouses = warehouses,
            AddressDeliveryAllowed = s.AddressDeliveryAllowed ?? true,
            StreetsAvailability = s.StreetsAvailability ?? false
        };
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

    /// <summary>Довідник типів складів НП (<c>getWarehouseTypes</c>), з кешем 24 год.</summary>
    public async Task<List<WarehouseTypeDto>> GetWarehouseTypesAsync(bool forceRefresh = false)
    {
        var snap = await EnsureWarehouseTypesSnapshotAsync(forceRefresh).ConfigureAwait(false);
        return [.. snap.Types];
    }

    public async Task<List<WarehouseDto>> GetPostomatsBySettlementAsync(string settlementRef, string? findByString = null)
    {
        var typesSnap = await EnsureWarehouseTypesSnapshotAsync(forceRefresh: false).ConfigureAwait(false);
        var all = await FetchWarehousesForCityAsync(settlementRef, findByString).ConfigureAwait(false);
        return [.. all.Where(w => ClassifyWarehouseAsPostomat(w, typesSnap))];
    }

    public async Task<List<WarehouseDto>> GetBranchWarehousesBySettlementAsync(string settlementRef, string? findByString = null)
    {
        var typesSnap = await EnsureWarehouseTypesSnapshotAsync(forceRefresh: false).ConfigureAwait(false);
        var all = await FetchWarehousesForCityAsync(settlementRef, findByString).ConfigureAwait(false);
        return [.. all.Where(w => !ClassifyWarehouseAsPostomat(w, typesSnap))];
    }

    /// <remarks>
    /// Якщо <see cref="WarehouseDto.TypeOfWarehouseRef"/> є в довіднику НП — вирішуємо за описом типу («поштомат» у UA/RU тексті).
    /// Інакше — за <see cref="WarehouseDto.CategoryOfWarehouse"/> та евристикою назви.
    /// </remarks>
    private static bool ClassifyWarehouseAsPostomat(WarehouseDto w, WarehouseTypesSnapshot typesSnap)
    {
        var tid = w.TypeOfWarehouseRef?.Trim();
        if (!string.IsNullOrEmpty(tid) && typesSnap.KnownRefs.Contains(tid))
        {
            return typesSnap.PostomatRefs.Contains(tid);
        }

        return FallbackGuessPostomatWithoutKnownTypeRef(w);
    }

    private static bool FallbackGuessPostomatWithoutKnownTypeRef(WarehouseDto w)
    {
        if (!string.IsNullOrEmpty(w.CategoryOfWarehouse)
            && w.CategoryOfWarehouse.Equals("Postomat", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(w.CategoryOfWarehouse)
            && w.CategoryOfWarehouse.Equals("Branch", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var d = w.Description ?? string.Empty;
        return d.Contains("Поштомат", StringComparison.OrdinalIgnoreCase)
            || d.Contains("Почтомат", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<List<WarehouseDto>> FetchWarehousesForCityAsync(string cityRef, string? findByString)
    {
        if (string.IsNullOrWhiteSpace(cityRef))
        {
            return [];
        }

        var key = cityRef.Trim();
        var find = string.IsNullOrWhiteSpace(findByString) ? null : findByString.Trim();

        // getWarehouses очікує CityRef з поля deliveryCity (пошук міст), а у шляху API часто дають SettlementRef — спочатку мапимо через getSettlements.
        var resolved = await TryResolveDeliveryCityRefForGetWarehousesAsync(key).ConfigureAwait(false);
        var effectiveCityRef = string.IsNullOrWhiteSpace(resolved) ? key : resolved.Trim();

        return await FetchWarehousesForCityOnceAsync(effectiveCityRef, find).ConfigureAwait(false);
    }

    /// <summary>Один прохід getWarehouses (усі сторінки, якщо без FindByString).</summary>
    private async Task<List<WarehouseDto>> FetchWarehousesForCityOnceAsync(string cityRef, string? findByString)
    {
        if (!string.IsNullOrWhiteSpace(findByString))
        {
            return await CallGetWarehousesAddressGeneralAsync(
                cityRef,
                findByString,
                page: 1,
                limit: WarehouseFetchLimit);
        }

        var all = new List<WarehouseDto>();
        for (var page = 1; page <= MaxWarehousePages; page++)
        {
            var batch = await CallGetWarehousesAddressGeneralAsync(cityRef, findByString: null, page, WarehouseFetchLimit);
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

    /// <summary>
    /// За <c>getSettlements(Ref=…)</c> повертає <c>DeliveryCity</c> для параметра <c>CityRef</c> у <c>getWarehouses</c>.
    /// Якщо Ref уже «міський» і рядок не знайдено — <c>null</c>, тоді використовуємо початковий ref.
    /// </summary>
    private async Task<string?> TryResolveDeliveryCityRefForGetWarehousesAsync(string settlementOrCityRef)
    {
        if (string.IsNullOrWhiteSpace(settlementOrCityRef))
        {
            return null;
        }

        var request = new NovaPoshtaRequest
        {
            ApiKey = _apiKey,
            ModelName = "AddressGeneral",
            CalledMethod = "getSettlements",
            MethodProperties = new { Page = 1, Ref = settlementOrCityRef.Trim() }
        };

        var response = await SendRequestAsync<NovaPoshtaResponse<List<SettlementDirectoryDto>>>(request)
            .ConfigureAwait(false);

        if (!response.Success || response.Data is not { Count: > 0 } rows)
        {
            return null;
        }

        var delivery = rows[0].DeliveryCity;
        return string.IsNullOrWhiteSpace(delivery) ? null : delivery.Trim();
    }

    private async Task<WarehouseTypesSnapshot> EnsureWarehouseTypesSnapshotAsync(bool forceRefresh)
    {
        await WarehouseTypesLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var now = DateTimeOffset.UtcNow;
            if (!forceRefresh
                && _warehouseTypesCache != null
                && now - _warehouseTypesFetchedUtc < WarehouseTypesCacheTtl)
            {
                return _warehouseTypesCache;
            }

            var list = await FetchWarehouseTypesFromApiAsync().ConfigureAwait(false);
            if (list.Count == 0 && _warehouseTypesCache != null)
            {
                logger.LogWarning("Nova Poshta getWarehouseTypes returned empty; keeping previous cache.");
                return _warehouseTypesCache;
            }

            _warehouseTypesCache = WarehouseTypesSnapshot.FromApiList(list);
            _warehouseTypesFetchedUtc = now;
            return _warehouseTypesCache;
        }
        finally
        {
            WarehouseTypesLock.Release();
        }
    }

    private async Task<List<WarehouseTypeDto>> FetchWarehouseTypesFromApiAsync()
    {
        var envelope = new JsonObject
        {
            ["apiKey"] = _apiKey,
            ["modelName"] = "AddressGeneral",
            ["calledMethod"] = "getWarehouseTypes",
            ["methodProperties"] = new JsonObject()
        };

        var jsonBody = envelope.ToJsonString(NovaPoshtaOutboundJson);
        var response = await PostNovaPoshtaJsonAsync<NovaPoshtaResponse<List<WarehouseTypeDto>>>(
            jsonBody,
            "AddressGeneral.getWarehouseTypes");

        if (!response.Success || response.Data == null)
        {
            logger.LogWarning(
                "Nova Poshta getWarehouseTypes failed: {Errors}",
                response.Errors is { Count: > 0 } errList ? string.Join("; ", errList) : "(no errors)");
            return [];
        }

        return response.Data;
    }

    private sealed record WarehouseTypesSnapshot(
        List<WarehouseTypeDto> Types,
        HashSet<string> KnownRefs,
        HashSet<string> PostomatRefs)
    {
        public static WarehouseTypesSnapshot FromApiList(List<WarehouseTypeDto> apiTypes)
        {
            var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pm = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var t in apiTypes)
            {
                if (string.IsNullOrWhiteSpace(t.Ref))
                {
                    continue;
                }

                var r = t.Ref.Trim();
                known.Add(r);

                if (TypeDescriptionsIndicatePostomat(t.Description, t.DescriptionRu))
                {
                    pm.Add(r);
                }
            }

            return new WarehouseTypesSnapshot([.. apiTypes], known, pm);
        }
    }

    private static bool TypeDescriptionsIndicatePostomat(string? ua, string? ru)
    {
        foreach (var s in new[] { ua, ru })
        {
            if (string.IsNullOrEmpty(s))
            {
                continue;
            }

            if (s.Contains("поштомат", StringComparison.OrdinalIgnoreCase)
                || s.Contains("почтомат", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Сторінка відділень/складів через <c>AddressGeneral.getWarehouses</c> (<c>Page</c>/<c>Limit</c> як у НП).</summary>
    public async Task<List<WarehouseDto>> GetWarehousesByCityAsync(string cityRef, int page = 1, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(cityRef))
        {
            return [];
        }

        var p = page < 1 ? 1 : page;
        var lim = limit < 1 ? 50 : limit;

        return await CallGetWarehousesAddressGeneralAsync(cityRef.Trim(), findByString: null, p, lim);
    }

    /// <inheritdoc />
    public async Task<WarehouseDto?> GetWarehouseByCityAndRefAsync(string deliveryCityRef, string warehouseRef)
    {
        if (string.IsNullOrWhiteSpace(deliveryCityRef) || string.IsNullOrWhiteSpace(warehouseRef))
        {
            return null;
        }

        var city = deliveryCityRef.Trim();
        var want = warehouseRef.Trim();

        for (var page = 1; page <= MaxWarehousePages; page++)
        {
            var batch = await GetWarehousesByCityAsync(city, page, WarehouseFetchLimit).ConfigureAwait(false);
            if (batch.Count == 0)
            {
                break;
            }

            foreach (var row in batch)
            {
                if (row.Ref.Equals(want, StringComparison.OrdinalIgnoreCase))
                {
                    return row;
                }
            }

            if (batch.Count < WarehouseFetchLimit)
            {
                break;
            }
        }

        logger.LogInformation(
            "Nova Poshta: warehouse/postomat Ref {WarehouseRef} not found for CityRef={CityRef}.",
            want,
            city);

        return null;
    }

    private async Task<List<WarehouseDto>> CallGetWarehousesAddressGeneralAsync(
        string cityRef,
        string? findByString,
        int? page,
        int? limit)
    {
        var methodProps = new JsonObject { ["CityRef"] = cityRef };
        if (!string.IsNullOrWhiteSpace(findByString))
        {
            methodProps["FindByString"] = findByString;
        }
        else
        {
            // Повне завантаження відділень міста — пагінація; пошук за рядком іде лише з CityRef + FindByString (док. НП).
            var effectivePage = page is > 0 ? page.Value : 1;
            var effectiveLimit = limit is > 0 ? limit.Value : WarehouseFetchLimit;
            methodProps["Page"] = effectivePage.ToString(CultureInfo.InvariantCulture);
            methodProps["Limit"] = effectiveLimit.ToString(CultureInfo.InvariantCulture);
        }

        var envelope = new JsonObject
        {
            ["apiKey"] = _apiKey,
            ["modelName"] = "AddressGeneral",
            ["calledMethod"] = "getWarehouses",
            ["methodProperties"] = methodProps
        };

        var jsonBody = envelope.ToJsonString(NovaPoshtaOutboundJson);

        var response = await PostNovaPoshtaJsonAsync<NovaPoshtaResponse<List<WarehouseDto>>>(
            jsonBody,
            "AddressGeneral.getWarehouses");

        if (!response.Success || response.Data == null)
        {
            logger.LogWarning(
                "Nova Poshta getWarehouses failed (CityRef={CityRef}, Find={Find}): {Errors}",
                cityRef,
                findByString ?? "(none)",
                response.Errors is { Count: > 0 } errList ? string.Join("; ", errList) : "(no errors)");
            return [];
        }

        return response.Data;
    }

    public async Task<List<StreetDto>> SearchStreetsAsync(string settlementRef, string streetQuery, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(settlementRef) || string.IsNullOrWhiteSpace(streetQuery))
        {
            return [];
        }

        var limitClamped = limit > 0 ? limit : 20;

        var methodProps = new JsonObject
        {
            ["StreetName"] = streetQuery.Trim(),
            ["SettlementRef"] = settlementRef.Trim()
        };

        var envelope = new JsonObject
        {
            ["apiKey"] = _apiKey,
            ["modelName"] = "AddressGeneral",
            ["calledMethod"] = "searchSettlementStreets",
            ["methodProperties"] = methodProps
        };

        var jsonBody = envelope.ToJsonString(NovaPoshtaOutboundJson);

        var response = await PostNovaPoshtaJsonAsync<NovaPoshtaResponse<List<CityStreetSearchResult>>>(
            jsonBody,
            "AddressGeneral.searchSettlementStreets");

        if (!response.Success || response.Data == null || response.Data.Count == 0)
        {
            if (!response.Success)
            {
                logger.LogWarning(
                    "Nova Poshta searchSettlementStreets failed for settlement {Ref}: {Errors}",
                    settlementRef,
                    response.Errors is { Count: > 0 } errList ? string.Join("; ", errList) : "(no errors)");
            }

            return [];
        }

        var streets = response.Data[0].Addresses ?? [];
        return [.. streets.Take(limitClamped)];
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

    private static JsonObject BuildRequestEnvelope(NovaPoshtaRequest request)
    {
        ArgumentNullException.ThrowIfNull(request.MethodProperties);

        var methodPropsNode = JsonSerializer.SerializeToNode(
            request.MethodProperties,
            request.MethodProperties.GetType(),
            NovaPoshtaOutboundJson) ?? throw new InvalidOperationException("Failed to serialize MethodProperties.");

        return new JsonObject
        {
            ["apiKey"] = request.ApiKey,
            ["modelName"] = request.ModelName,
            ["calledMethod"] = request.CalledMethod,
            ["methodProperties"] = methodPropsNode
        };
    }

    /// <summary>Маскує ключ у JSON для безпечного логування.</summary>
    private string MaskSensitiveApiKey(string jsonBody)
    {
        if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(jsonBody))
        {
            return jsonBody;
        }

        return jsonBody.Replace(_apiKey, "***", StringComparison.Ordinal);
    }

    private async Task<T> PostNovaPoshtaJsonAsync<T>(string jsonBody, string logLabel)
    {
        try
        {
            if (_logFullNovaPoshtaExchange)
            {
                logger.LogInformation(
                    "Nova Poshta {Label} REQUEST (masked): {Json}",
                    logLabel,
                    MaskSensitiveApiKey(jsonBody));
            }

            using var payload = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var httpResponse = await httpClient.PostAsync(ApiUrl, payload);

            httpResponse.EnsureSuccessStatusCode();

            var raw = await httpResponse.Content.ReadAsStringAsync();

            // Занадто шумно для звичайної роботи (тіло відповіді НП велике).
            // if (_logFullNovaPoshtaExchange)
            // {
            //     logger.LogInformation(
            //         "Nova Poshta {Label} RAW RESPONSE: {Raw}",
            //         logLabel,
            //         raw);
            // }

            var result = JsonSerializer.Deserialize<T>(raw, NovaPoshtaInboundJson);

            return result ?? throw new InvalidOperationException("Empty response from Nova Poshta API");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Nova Poshta API request failed ({Label})", logLabel);
            throw new ApiException("NOVA_POSHTA_ERROR", "Failed to communicate with Nova Poshta API", 500);
        }
    }

    private Task<T> SendRequestAsync<T>(NovaPoshtaRequest request)
    {
        var jsonBody = BuildRequestEnvelope(request).ToJsonString(NovaPoshtaOutboundJson);
        return PostNovaPoshtaJsonAsync<T>(jsonBody, $"{request.ModelName}.{request.CalledMethod}");
    }

    #endregion Helper Methods
}