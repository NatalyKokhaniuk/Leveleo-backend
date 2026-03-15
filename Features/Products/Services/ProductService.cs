using LeveLEO.Data;
using LeveLEO.Features.Inventory.Services;
using LeveLEO.Features.ProductAttributes.Models;
using LeveLEO.Features.Products.DTO;
using LeveLEO.Features.Products.Models;
using LeveLEO.Features.Promotions.DTO;
using LeveLEO.Features.Promotions.Models;
using LeveLEO.Features.Promotions.Services;
using LeveLEO.Infrastructure.Events;
using LeveLEO.Infrastructure.Events.DomainEvents;
using LeveLEO.Infrastructure.Media.Services;
using LeveLEO.Infrastructure.SlugGenerator;
using Microsoft.EntityFrameworkCore;

namespace LeveLEO.Features.Products.Services;

public class ProductService(AppDbContext db, IMediaService mediaService, IPromotionService promotionService, IInventoryService inventoryService, IEventBus eventBus) : IProductService
{
    private readonly AppDbContext _db = db;
    private readonly IMediaService _mediaService = mediaService;
    private readonly IPromotionService _promotionService = promotionService;
    private readonly IInventoryService _inventoryService = inventoryService;
    private readonly IEventBus _eventBus = eventBus;

    #region CRUD Categories

    public async Task<ProductResponseDto> CreateAsync(CreateProductDto dto)
    {
        var slug = SlugService.ToSlug(dto.Name);

        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            CategoryId = dto.CategoryId,
            BrandId = dto.BrandId,
            MainImageKey = dto.MainImageKey,
            StockQuantity = dto.StockQuantity,
            IsActive = dto.IsActive,
            Slug = slug,
            Translations = dto.Translations?.Select(t => new ProductTranslation
            {
                LanguageCode = t.LanguageCode,
                Name = t.Name,
                Description = t.Description
            }).ToList() ?? []
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        var aggregates = await GetAggregatesAsync(product.Id);
        await _eventBus.PublishAsync(new ProductCreatedEvent
        {
            ProductId = product.Id,
            ProductName = product.Name,
            ProductSlug = product.Slug,
            Price = product.Price,
            MainImageUrl = product.MainImageKey,
            ShortDescription = product.Description
        });
        return await MapToDtoAsync(product, aggregates.AverageRating, aggregates.TotalSold, aggregates.RatingCount);
    }

    public async Task<ProductResponseDto> UpdateAsync(Guid productId, UpdateProductDto dto)
    {
        var product = await _db.Products
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == productId)
            ?? throw new KeyNotFoundException($"Product with Id '{productId}' not found.");

        if (dto.Name.HasValue)
        {
            if (product.Name != dto.Name.Value && !string.IsNullOrEmpty(dto.Name.Value))
            {
                product.Slug = SlugService.ToSlug(dto.Name.Value);
                product.Name = dto.Name.Value;
            }
        }

        if (dto.Description.HasValue)
            product.Description = dto.Description.Value;
        if (dto.Price.HasValue)
            product.Price = dto.Price.Value;
        if (dto.CategoryId.HasValue)
            product.CategoryId = dto.CategoryId.Value;
        if (dto.BrandId.HasValue)
            product.BrandId = dto.BrandId.Value;
        if (dto.MainImageKey.HasValue)
            product.MainImageKey = dto.MainImageKey.Value;
        if (dto.StockQuantity.HasValue)
            product.StockQuantity = dto.StockQuantity.Value;
        if (dto.IsActive.HasValue)
            product.IsActive = dto.IsActive.Value;

        if (dto.Translations.HasValue && dto.Translations.Value != null)
        {
            foreach (var tr in dto.Translations.Value)
            {
                var existing = product.Translations.FirstOrDefault(t => t.LanguageCode == tr.LanguageCode);
                if (existing != null)
                {
                    existing.Name = tr.Name;
                    existing.Description = tr.Description;
                }
                else
                {
                    product.Translations.Add(new ProductTranslation
                    {
                        LanguageCode = tr.LanguageCode,
                        Name = tr.Name,
                        Description = tr.Description
                    });
                }
            }
        }

        await _db.SaveChangesAsync();

        var aggregates = await GetAggregatesAsync(product.Id);
        return await MapToDtoAsync(product, aggregates.AverageRating, aggregates.TotalSold, aggregates.RatingCount);
    }

    public async Task DeleteAsync(Guid productId)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var product = await _db.Products
            .Include(p => p.Images)
            .Include(p => p.Videos)
            .FirstOrDefaultAsync(p => p.Id == productId)
            ?? throw new ApiException("PRODUCT_NOT_FOUND", $"Product with Id '{productId}' not found.", 404);

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        await transaction.CommitAsync();

        foreach (var img in product.Images)
            await _mediaService.DeleteFileAsync(img.ImageKey);

        foreach (var vid in product.Videos)
            await _mediaService.DeleteFileAsync(vid.VideoKey);
    }

    public async Task<ProductResponseDto> GetByIdAsync(Guid productId)
    {
        var product = await _db.Products
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == productId)
            ?? throw new KeyNotFoundException($"Product with Id '{productId}' not found.");

        var aggregates = await GetAggregatesAsync(product.Id);
        return await MapToDtoAsync(product, aggregates.AverageRating, aggregates.TotalSold, aggregates.RatingCount);
    }

    public async Task<ProductResponseDto> GetBySlugAsync(string slug)
    {
        var product = await _db.Products
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Slug == slug)
            ?? throw new KeyNotFoundException($"Product with Slug '{slug}' not found.");

        var aggregates = await GetAggregatesAsync(product.Id);
        return await MapToDtoAsync(product, aggregates.AverageRating, aggregates.TotalSold, aggregates.RatingCount);
    }

    #endregion CRUD Categories

    #region Translations

    public async Task<ProductTranslationResponseDto> AddTranslationAsync(Guid productId, ProductTranslationDto dto)
    {
        var product = await _db.Products
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == productId)
            ?? throw new KeyNotFoundException($"Product with Id '{productId}' not found.");

        if (product.Translations.Any(t => t.LanguageCode == dto.LanguageCode))
            throw new InvalidOperationException($"Translation for language '{dto.LanguageCode}' already exists.");

        var translation = new ProductTranslation
        {
            ProductId = productId,
            LanguageCode = dto.LanguageCode,
            Name = dto.Name,
            Description = dto.Description
        };

        product.Translations.Add(translation);
        await _db.SaveChangesAsync();

        return new ProductTranslationResponseDto
        {
            Id = translation.Id,
            LanguageCode = translation.LanguageCode,
            Name = translation.Name,
            Description = translation.Description
        };
    }

    public async Task<ProductTranslationResponseDto> UpdateTranslationAsync(Guid productId, ProductTranslationDto dto)
    {
        var translation = await _db.ProductTranslations
            .FirstOrDefaultAsync(t => t.ProductId == productId && t.LanguageCode == dto.LanguageCode)
            ?? throw new KeyNotFoundException($"Translation for language '{dto.LanguageCode}' not found for product '{productId}'.");

        translation.Name = dto.Name;
        translation.Description = dto.Description;

        await _db.SaveChangesAsync();

        return new ProductTranslationResponseDto
        {
            Id = translation.Id,
            LanguageCode = translation.LanguageCode,
            Name = translation.Name,
            Description = translation.Description
        };
    }

    public async Task DeleteTranslationAsync(Guid productId, string languageCode)
    {
        var translation = await _db.ProductTranslations
            .FirstOrDefaultAsync(t => t.ProductId == productId && t.LanguageCode == languageCode)
            ?? throw new KeyNotFoundException($"Translation for language '{languageCode}' not found for product '{productId}'.");

        _db.ProductTranslations.Remove(translation);
        await _db.SaveChangesAsync();
    }

    #endregion Translations

    #region List & Search

    public async Task<PagedResultDto<ProductResponseDto>> GetAllAsync(ProductFilterDto? filter = null)
    {
        filter ??= new ProductFilterDto();

        var query = _db.Products
            .Include(p => p.Translations)
            .AsQueryable();

        query = await ApplyFilterAsync(query, filter);

        var queryWithAggregates = query
            .Select(p => new ProductWithAggregates
            {
                Product = p,
                AverageRating = p.OrderItems
    .Where(oi => oi.Review != null && oi.Review.IsApproved)
    .Any()
    ? p.OrderItems
        .Where(oi => oi.Review != null && oi.Review.IsApproved)
        .Average(oi => (decimal)oi.Review!.Rating)
    : 0m,

                TotalSold = p.OrderItems.Sum(oi => (int?)oi.Quantity) ?? 0,
                RatingCount = p.OrderItems
                    .Count(oi => oi.Review != null && oi.Review.IsApproved)
            });

        queryWithAggregates = filter.SortBy switch
        {
            ProductSortBy.PriceAsc => queryWithAggregates.OrderBy(x => x.Product.Price),
            ProductSortBy.PriceDesc => queryWithAggregates.OrderByDescending(x => x.Product.Price),
            ProductSortBy.AverageRatingDesc => queryWithAggregates.OrderByDescending(x => x.AverageRating),
            ProductSortBy.TotalSoldDesc => queryWithAggregates.OrderByDescending(x => x.TotalSold),
            _ => queryWithAggregates.OrderBy(x => x.Product.Price)
        };

        var totalCount = await queryWithAggregates.CountAsync();

        var items = await queryWithAggregates
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var dtos = await MapToDtosAsync(items.Select(x => x.Product));

        return new PagedResultDto<ProductResponseDto>
        {
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = dtos
        };
    }

    public async Task<PagedResultDto<ProductResponseDto>> SearchAsync(string query, int page = 1, int pageSize = 20)
    {
        var q = _db.Products
            .Include(p => p.Translations)
            .Where(p =>
                EF.Functions.ILike(p.Name, $"%{query}%") ||
                (p.Description != null && EF.Functions.ILike(p.Description, $"%{query}%")) ||
                p.Translations.Any(t =>
                    EF.Functions.ILike(t.Name, $"%{query}%") ||
                    (t.Description != null && EF.Functions.ILike(t.Description, $"%{query}%"))
                ));

        var totalCount = await q.CountAsync();

        var queryWithAggregates = q
            .Select(p => new ProductWithAggregates
            {
                Product = p,
                AverageRating = p.OrderItems
    .Where(oi => oi.Review != null && oi.Review.IsApproved)
    .Any()
    ? p.OrderItems
        .Where(oi => oi.Review != null && oi.Review.IsApproved)
        .Average(oi => (decimal)oi.Review!.Rating)
    : 0m,

                TotalSold = p.OrderItems.Sum(oi => (int?)oi.Quantity) ?? 0,
                RatingCount = p.OrderItems
                    .Count(oi => oi.Review != null && oi.Review.IsApproved)
            });

        var items = await queryWithAggregates
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = await MapToDtosAsync(items.Select(x => x.Product));

        return new PagedResultDto<ProductResponseDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = dtos
        };
    }

    #endregion List & Search

    #region Aggregates Calculation

    private async Task<ProductAggregates> GetAggregatesAsync(Guid productId)
    {
        var aggregates = await _db.Products
            .Where(p => p.Id == productId)
            .Select(p => new ProductAggregates
            {
                AverageRating = p.OrderItems
    .Where(oi => oi.Review != null && oi.Review.IsApproved)
    .Any()
    ? p.OrderItems
        .Where(oi => oi.Review != null && oi.Review.IsApproved)
        .Average(oi => (decimal)oi.Review!.Rating)
    : 0m,

                TotalSold = p.OrderItems.Sum(oi => (int?)oi.Quantity) ?? 0,
                RatingCount = p.OrderItems
                    .Count(oi => oi.Review != null && oi.Review.IsApproved)
            }).FirstOrDefaultAsync();

        return aggregates ?? new ProductAggregates { AverageRating = 0m, TotalSold = 0, RatingCount = 0 };
    }

    private async Task<ProductResponseDto> MapToDtoAsync(
            Product product,
            decimal averageRating,
            int totalSold,
            int ratingCount)
    {
        var (discountedPrice, promotion) = await _promotionService.GetBestProductPromotionAsync(product);
        var availableQuantity = await _inventoryService.GetAvailableQuantityAsync(product.Id);

        var dto = new ProductResponseDto
        {
            Id = product.Id,
            Slug = product.Slug,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            DiscountedPrice = discountedPrice,
            AppliedPromotion = promotion == null ? null : new AppliedPromotionDto
            {
                Id = promotion.Id,
                Slug = promotion.Slug,
                Name = promotion.Name,
                Description = promotion.Description,
                ImageKey = promotion.ImageKey,
                Level = promotion.Level,
                DiscountType = promotion.DiscountType!.Value,
                DiscountValue = promotion.DiscountValue!.Value,
                StartDate = promotion.StartDate,
                EndDate = promotion.EndDate,
                IsCoupon = promotion.IsCoupon,
                IsPersonal = promotion.IsPersonal,
                CouponCode = promotion.CouponCode,
                Translations = [.. promotion.Translations
                .Select(t => new PromotionTranslationDto
                {
                    LanguageCode = t.LanguageCode,
                    Name = t.Name,
                    Description = t.Description
                })]
            },
            MainImageKey = product.MainImageKey,
            StockQuantity = product.StockQuantity,
            AvailableQuantity = availableQuantity,
            IsActive = product.IsActive,
            CategoryId = product.CategoryId,
            BrandId = product.BrandId,
            AverageRating = averageRating,
            TotalSold = totalSold,
            RatingCount = ratingCount,
            Translations = [.. product.Translations.Select(t => new ProductTranslationResponseDto
            {
                Id = t.Id,
                LanguageCode = t.LanguageCode,
                Name = t.Name,
                Description = t.Description
            })]
        };
        return dto;
    }

    private async Task<List<ProductResponseDto>> MapToDtosAsync(IEnumerable<Product> products)
    {
        var productList = products.ToList();
        var productIds = productList.Select(p => p.Id).ToList();

        // 1️⃣ Агрегати для всіх продуктів одним запитом
        var aggregates = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                AverageRating = p.OrderItems
    .Where(oi => oi.Review != null && oi.Review.IsApproved)
    .Any()
    ? p.OrderItems
        .Where(oi => oi.Review != null && oi.Review.IsApproved)
        .Average(oi => (decimal)oi.Review!.Rating)
    : 0m,

                TotalSold = p.OrderItems.Sum(oi => (int?)oi.Quantity) ?? 0,
                RatingCount = p.OrderItems // 👈 ДОДАНО
                    .Count(oi => oi.Review != null && oi.Review.IsApproved)
            })
            .ToListAsync();

        var aggregateDict = aggregates.ToDictionary(a => a.Id);

        // 2️⃣ Промо для всіх продуктів
        var promotionsDict = await _promotionService.GetBestProductPromotionsAsync(productList);

        // 3️⃣ Доступна кількість для всіх продуктів (batch)
        var availabilityDict = await _inventoryService.GetAvailableQuantitiesAsync(productIds);

        // 4️⃣ Map
        var result = new List<ProductResponseDto>();
        foreach (var product in productList)
        {
            var agg = aggregateDict.GetValueOrDefault(product.Id);
            var (discountedPrice, promotion) = promotionsDict.GetValueOrDefault(product.Id, (null, null));
            var availableQuantity = availabilityDict.GetValueOrDefault(product.Id, 0);

            result.Add(new ProductResponseDto
            {
                Id = product.Id,
                Slug = product.Slug,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                DiscountedPrice = discountedPrice,
                AppliedPromotion = promotion == null ? null : new AppliedPromotionDto
                {
                    Id = promotion.Id,
                    Slug = promotion.Slug,
                    Name = promotion.Name,
                    Description = promotion.Description,
                    ImageKey = promotion.ImageKey,
                    Level = promotion.Level,
                    DiscountType = promotion.DiscountType!.Value,
                    DiscountValue = promotion.DiscountValue!.Value,
                    StartDate = promotion.StartDate,
                    EndDate = promotion.EndDate,
                    IsCoupon = promotion.IsCoupon,
                    IsPersonal = promotion.IsPersonal,
                    CouponCode = promotion.CouponCode,
                    Translations = [.. promotion.Translations
                    .Select(t => new PromotionTranslationDto
                    {
                        LanguageCode = t.LanguageCode,
                        Name = t.Name,
                        Description = t.Description
                    })]
                },
                MainImageKey = product.MainImageKey,
                StockQuantity = product.StockQuantity,
                AvailableQuantity = availableQuantity,
                IsActive = product.IsActive,
                CategoryId = product.CategoryId,
                BrandId = product.BrandId,
                AverageRating = agg?.AverageRating ?? 0m,
                TotalSold = agg?.TotalSold ?? 0,
                RatingCount = agg?.RatingCount ?? 0,
                Translations = [.. product.Translations.Select(t => new ProductTranslationResponseDto
            {
                Id = t.Id,
                LanguageCode = t.LanguageCode,
                Name = t.Name,
                Description = t.Description
            })]
            });
        }

        return result;
    }

    private async Task<IQueryable<Product>> ApplyFilterAsync(
        IQueryable<Product> query,
        ProductFilterDto filter)
    {
        if (!filter.IncludeInactive)
            query = query.Where(p => p.IsActive);

        if (filter.BrandId.HasValue)
            query = query.Where(p => p.BrandId == filter.BrandId.Value);

        if (filter.PriceFrom.HasValue)
            query = query.Where(p => p.Price >= filter.PriceFrom.Value);

        if (filter.PriceTo.HasValue)
            query = query.Where(p => p.Price <= filter.PriceTo.Value);

        if (filter.CategoryId.HasValue)
        {
            var categoryId = filter.CategoryId.Value;

            query = query.Where(p =>
                _db.CategoryClosures.Any(c =>
                    c.AncestorId == categoryId &&
                    c.DescendantId == p.CategoryId)
                || p.CategoryId == categoryId);
        }
        // ===============================
        // ===== ДОДАНО ФІЛЬТР ЗА АКЦІЄЮ =====
        // ===============================
        if (filter.PromotionId.HasValue)
        {
            var promoId = filter.PromotionId.Value;

            // Отримуємо конкретну акцію разом із перекладами
            var promotion = await _db.Promotions
                .Include(p => p.Translations)
                .FirstOrDefaultAsync(p => p.Id == promoId);

            if (promotion != null && promotion.Level == PromotionLevel.Product)
            {
                // Продукти, що підпадають під умови акції
                var productIds = new List<Guid>();

                // Якщо задані конкретні продукти
                if (promotion.ProductConditions?.ProductIds.HasValue == true &&
                    promotion.ProductConditions.ProductIds.Value != null)
                {
                    productIds.AddRange(promotion.ProductConditions.ProductIds.Value);
                }

                // Якщо задані категорії (включно з дочірніми)
                if (promotion.ProductConditions?.CategoryIds.HasValue == true &&
                    promotion.ProductConditions.CategoryIds.Value != null &&
                    promotion.ProductConditions.CategoryIds.Value.Count > 0)
                {
                    var catIds = promotion.ProductConditions.CategoryIds.Value;

                    var descendantCategoryIds = await _db.CategoryClosures
                        .Where(c => catIds.Contains(c.AncestorId))
                        .Select(c => c.DescendantId)
                        .ToListAsync();

                    var categoryProductIds = await _db.Products
                        .Where(p => descendantCategoryIds.Contains(p.CategoryId))
                        .Select(p => p.Id)
                        .ToListAsync();

                    productIds.AddRange(categoryProductIds);
                }

                // Виключаємо дублі
                productIds = [.. productIds.Distinct()];

                // Фільтруємо запит
                query = query.Where(p => productIds.Contains(p.Id));
            }
        }
        // ===============================
        // НОВА ТИПІЗОВАНА ФІЛЬТРАЦІЯ З АТРИБУТАМИ
        // ===============================
        if (filter.AttributeFilters != null && filter.AttributeFilters.Count != 0)
        {
            var filterDict = filter.AttributeFilters
                .GroupBy(f => f.AttributeId)
                .ToDictionary(g => g.Key, g => g.First());

            var attributeIds = filterDict.Keys.ToList();

            var attributes = await _db.ProductAttributes
                .Where(a => attributeIds.Contains(a.Id))
                .ToListAsync();

            foreach (var attribute in attributes)
            {
                if (!attribute.IsFilterable)
                    continue;

                if (!filterDict.TryGetValue(attribute.Id, out var filterItem))
                    continue;

                switch (attribute.Type)
                {
                    case AttributeType.String:
                        if (!filterItem.StringValues.HasValue || filterItem.StringValues.Value is null || filterItem.StringValues.Value.Count == 0)
                            break;

                        var stringValues = filterItem.StringValues.Value;

                        query = query.Where(p =>
                            p.AttributeValues.Any(av =>
                                av.ProductAttributeId == attribute.Id &&
                                av.StringValue != null &&
                                stringValues.Contains(av.StringValue)));
                        break;

                    case AttributeType.Decimal:
                        if (!filterItem.DecimalValues.HasValue || filterItem.DecimalValues.Value is null || filterItem.DecimalValues.Value.Count == 0)
                            break;

                        var decimalValues = filterItem.DecimalValues.Value;

                        query = query.Where(p =>
                            p.AttributeValues.Any(av =>
                                av.ProductAttributeId == attribute.Id &&
                                av.DecimalValue.HasValue &&
                                decimalValues.Contains(av.DecimalValue.Value)));
                        break;

                    case AttributeType.Integer:
                        if (!filterItem.IntegerValues.HasValue || filterItem.IntegerValues.Value is null || filterItem.IntegerValues.Value.Count == 0)
                            break;

                        var intValues = filterItem.IntegerValues.Value;

                        query = query.Where(p =>
                            p.AttributeValues.Any(av =>
                                av.ProductAttributeId == attribute.Id &&
                                av.IntValue.HasValue &&
                                intValues.Contains(av.IntValue.Value)));
                        break;

                    case AttributeType.Boolean:
                        if (!filterItem.BooleanValues.HasValue || filterItem.BooleanValues.Value is null || filterItem.BooleanValues.Value.Count == 0)
                            break;

                        var boolValues = filterItem.BooleanValues.Value;

                        query = query.Where(p =>
                            p.AttributeValues.Any(av =>
                                av.ProductAttributeId == attribute.Id &&
                                av.BoolValue.HasValue &&
                                boolValues.Contains(av.BoolValue.Value)));
                        break;
                }
            }
        }

        return query;
    }

    #endregion Aggregates Calculation

    #region Helpers

    private class ProductWithAggregates
    {
        public Product Product { get; set; } = null!;
        public decimal AverageRating { get; set; }
        public int TotalSold { get; set; }
        public int RatingCount { get; set; }
    }

    private class ProductAggregates
    {
        public decimal AverageRating { get; set; }
        public int TotalSold { get; set; }
        public int RatingCount { get; set; } // 👈 ДОДАНО
    }

    public async Task<ProductResponseDto> BuildFullDtoAsync(Guid productId)
    {
        var product = await _db.Products
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == productId)
            ?? throw new KeyNotFoundException($"Product {productId} not found");

        var aggregates = await GetAggregatesAsync(productId);

        return await MapToDtoAsync(product, aggregates.AverageRating, aggregates.TotalSold, aggregates.RatingCount);
    }

    public async Task<List<ProductResponseDto>> BuildFullDtosAsync(IEnumerable<Guid> productIds)
    {
        var ids = productIds.ToList();

        var products = await _db.Products
            .Include(p => p.Translations)
            .Where(p => ids.Contains(p.Id))
            .ToListAsync();

        return await MapToDtosAsync(products);
    }

    #endregion Helpers
}