namespace LeveLEO.Infrastructure.Delivery.DTO;

/// <summary>Елемент відповіді <c>AddressGeneral.getWarehouseTypes</c>.</summary>
public sealed class WarehouseTypeDto
{
    public string Ref { get; set; } = null!;
    public string? Description { get; set; }
    public string? DescriptionRu { get; set; }
}
