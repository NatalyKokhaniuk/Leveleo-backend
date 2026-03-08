namespace LeveLEO.Infrastructure.Delivery.DTO;

/// <summary>
/// Результат видалення експрес-накладної
/// </summary>
public class DeleteDocumentResult
{
    /// <summary>
    /// Ref видаленої накладної
    /// </summary>
    public string Ref { get; set; } = null!;

    /// <summary>
    /// Чи успішно видалено
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Повідомлення про результат
    /// </summary>
    public string? Message { get; set; }
}