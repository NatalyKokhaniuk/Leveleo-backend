namespace LeveLEO.Infrastructure.Delivery.DTO;

public class NovaPoshtaRequest
{
    public string ApiKey { get; set; } = null!;
    public string ModelName { get; set; } = null!;
    public string CalledMethod { get; set; } = null!;
    public object MethodProperties { get; set; } = null!;
}