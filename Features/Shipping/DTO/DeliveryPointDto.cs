namespace LeveLEO.Features.Shipping.DTO;

public class DeliveryPointDto
{
    public string Ref { get; set; } = null!;
    public string Type { get; set; } = null!; // Branch | Postomat
    public string Name { get; set; } = null!;
    public string ShortAddress { get; set; } = null!;
    public string CityRef { get; set; } = null!;
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
}
