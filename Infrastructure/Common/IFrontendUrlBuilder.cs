namespace LeveLEO.Infrastructure.Common;

public interface IFrontendUrlBuilder
{
    string BaseUrl { get; }

    string Build(string relativePath);

    string Home();

    string Order(Guid orderId);

    string Product(string slug);

    string ProductById(Guid productId);

    string Promotions();

    string MyReviews();

    string NewsletterUnsubscribe(string token);

    string PlaceholderImage();
}
