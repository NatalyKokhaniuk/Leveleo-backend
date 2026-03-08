namespace LeveLEO.Infrastructure.Delivery.DTO;

public class NovaPoshtaResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
    public List<string>? Warnings { get; set; }
    public List<string>? Info { get; set; }
}