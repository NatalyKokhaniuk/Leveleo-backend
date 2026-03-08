namespace LeveLEO.Features.Orders.DTO;

public class ProductReviewsDto
{
    public Guid ProductId { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int FiveStarCount { get; set; }
    public int FourStarCount { get; set; }
    public int ThreeStarCount { get; set; }
    public int TwoStarCount { get; set; }
    public int OneStarCount { get; set; }

    public List<ReviewResponseDto> Reviews { get; set; } = [];
}