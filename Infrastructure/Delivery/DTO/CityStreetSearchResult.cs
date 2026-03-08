namespace LeveLEO.Infrastructure.Delivery.DTO;

public class CityStreetSearchResult
{
    public int TotalCount { get; set; }
    public List<StreetDto>? Addresses { get; set; }
}