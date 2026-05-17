namespace LeveLEO.Infrastructure.Common;

public class FrontendUrlBuilder(IConfiguration configuration) : IFrontendUrlBuilder
{
    public string BaseUrl { get; } = ResolveBaseUrl(configuration);

    public string Build(string relativePath)
    {
        var path = relativePath.TrimStart('/');
        return string.IsNullOrEmpty(path) ? BaseUrl : $"{BaseUrl}/{path}";
    }

    public string Home() => BaseUrl;

    public string Order(Guid orderId) => Build($"orders/{orderId}");

    public string Product(string slug) => Build($"products/{slug}");

    public string ProductById(Guid productId) => Build($"products/{productId}");

    public string Promotions() => Build("promotions");

    public string MyReviews() => Build("my-reviews");

    public string NewsletterUnsubscribe(string token) =>
        Build($"newsletter/unsubscribe?token={Uri.EscapeDataString(token)}");

    public string PlaceholderImage() => Build("images/placeholder.jpg");

    private static string ResolveBaseUrl(IConfiguration configuration)
    {
        var url = configuration["Frontend:Url"]
            ?? throw new InvalidOperationException("Frontend:Url is not configured.");

        return url.TrimEnd('/');
    }
}
