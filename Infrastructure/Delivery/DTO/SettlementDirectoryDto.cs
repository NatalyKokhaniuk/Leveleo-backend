namespace LeveLEO.Infrastructure.Delivery.DTO;

/// <summary>
/// Запис з довідника населених пунктів НП (getSettlements).
/// </summary>
public class SettlementDirectoryDto
{
    public string Ref { get; set; } = null!;

    /// <summary>Повна назва з областю/районом.</summary>
    public string Description { get; set; } = null!;

    public string? DescriptionRu { get; set; }

    public string? Area { get; set; }

    public string? AreaDescription { get; set; }

    public string? Region { get; set; }

    public string? RegionsDescription { get; set; }

    public string? SettlementType { get; set; }

    /// <summary>Чи є відділення в населеному пункті (зазвичай "0"/"1").</summary>
    public string? Warehouse { get; set; }

    public string? Index { get; set; }
}
