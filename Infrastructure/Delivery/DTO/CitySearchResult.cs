namespace LeveLEO.Infrastructure.Delivery.DTO;

public class CitySearchResult
{
    public int TotalCount { get; set; }
    public List<CityDto>? Addresses { get; set; }
}