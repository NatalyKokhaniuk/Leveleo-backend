namespace LeveLEO.Infrastructure.Caching;

/// <summary>
/// Константи для ключів кешу (для уникнення дублювання і помилок)
/// </summary>
public static class CacheKeys
{
    // Продукти
    public static string Product(Guid productId) => $"product:{productId}";
    public static string ProductBySlug(string slug) => $"product:slug:{slug}";
    public static string ProductList(int page, int pageSize) => $"products:list:{page}:{pageSize}";
    public const string ProductsPattern = "product:*";

    // Категорії
    public static string Category(Guid categoryId) => $"category:{categoryId}";
    public static string CategoryBySlug(string slug) => $"category:slug:{slug}";
    public static string CategoryTree => "categories:tree";
    public const string CategoriesPattern = "category:*";

    // Бренди
    public static string Brand(Guid brandId) => $"brand:{brandId}";
    public static string BrandBySlug(string slug) => $"brand:slug:{slug}";
    public static string BrandsList => "brands:list";
    public const string BrandsPattern = "brand:*";

    // Відгуки
    public static string ProductReviews(Guid productId, int page) => $"reviews:product:{productId}:page:{page}";
    public static string FeaturedReviews => "reviews:featured";
    public const string ReviewsPattern = "reviews:*";

    // Статистика (короткий TTL)
    public const string DashboardStats = "stats:dashboard";
    public static string MonthlySales(int year) => $"stats:sales:monthly:{year}";
    public static string TopProducts(int days) => $"stats:products:top:{days}";
    public const string StatsPattern = "stats:*";

    // Кошик (персональний, довший TTL)
    public static string UserCart(string userId) => $"cart:user:{userId}";

    // Промоакції
    public static string ActivePromotions => "promotions:active";
    public static string Promotion(Guid promotionId) => $"promotion:{promotionId}";
    public const string PromotionsPattern = "promotion:*";

    // TTL (Time To Live) для різних типів даних
    public static class Ttl
    {
        public static readonly TimeSpan Product = TimeSpan.FromMinutes(30);
        public static readonly TimeSpan Category = TimeSpan.FromHours(1);
        public static readonly TimeSpan Brand = TimeSpan.FromHours(2);
        public static readonly TimeSpan Reviews = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan Stats = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan Cart = TimeSpan.FromHours(24);
        public static readonly TimeSpan Promotions = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan FeaturedReviews = TimeSpan.FromMinutes(30);
    }
}
