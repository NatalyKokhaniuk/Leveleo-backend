namespace LeveLEO.Infrastructure.Delivery.DTO;

public class StreetDto
{
    public string Ref { get; set; } = null!;
    public string Present { get; set; } = null!; // "вул. Хрещатик"
    public string StreetsType { get; set; } = null!; // "вул."
    public string StreetsTypeDescription { get; set; } = null!; // "вулиця"
}