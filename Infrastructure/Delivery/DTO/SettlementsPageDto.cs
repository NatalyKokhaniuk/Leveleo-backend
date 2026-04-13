namespace LeveLEO.Infrastructure.Delivery.DTO;

public class SettlementsPageDto
{
    public int Page { get; set; }

    /// <summary>Фактичний розмір сторінки з API НП (зазвичай до 150).</summary>
    public int PageSize { get; set; }

    public IReadOnlyList<SettlementDirectoryDto> Items { get; set; } = [];

    /// <summary>Ймовірно є наступна сторінка (якщо повернуто повну сторінку).</summary>
    public bool HasMore { get; set; }
}
