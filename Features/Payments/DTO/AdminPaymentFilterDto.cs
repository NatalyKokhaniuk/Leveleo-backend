using LeveLEO.Features.Payments.Models;

namespace LeveLEO.Features.Payments.DTO;

public class AdminPaymentFilterDto
{
    public PaymentStatus? Status { get; set; }

    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }

    /// <summary>Сортування: CreatedAt, Amount, Status, ExpireAt</summary>
    public string SortBy { get; set; } = "CreatedAt";

    public string SortDirection { get; set; } = "desc";

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
