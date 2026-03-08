using LeveLEO.Infrastructure.Common;

namespace LeveLEO.Features.Orders.DTO;

public class UpdateReviewDto
{
    public Optional<int> Rating { get; set; }
    public Optional<string?> Comment { get; set; }
    public Optional<List<string>?> PhotoKeys { get; set; }
    public Optional<List<string>?> VideoKeys { get; set; }
}