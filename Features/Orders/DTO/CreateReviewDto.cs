namespace LeveLEO.Features.Orders.DTO;

public class CreateReviewDto
{
    public Guid OrderItemId { get; set; }
    public int Rating { get; set; } // 1-5
    public string? Comment { get; set; }
    public List<string>? PhotoKeys { get; set; } // S3 keys для фото
    public List<string>? VideoKeys { get; set; } // S3 keys для відео
}