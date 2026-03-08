namespace LeveLEO.Features.Orders.DTO;

public class ReviewResponseDto
{
    public Guid Id { get; set; }
    public Guid OrderItemId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsApproved { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public List<ReviewPhotoDto> Photos { get; set; } = [];
    public List<ReviewVideoDto> Videos { get; set; } = [];
}