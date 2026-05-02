using System.Text.Json;

namespace LeveLEO.Infrastructure.Delivery.DTO;

public class NovaPoshtaResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
    public List<string>? Warnings { get; set; }

    /// <summary>
    /// НП у різних методах віддає <c>info</c> як масив рядків або як об'єкт (наприклад <c>{"totalCount":2}</c>).
    /// </summary>
    public JsonElement? Info { get; set; }
}