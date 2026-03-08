namespace LeveLEO.Infrastructure.Delivery.DTO;

/// <summary>
/// Інформація про статус посилки при трекінгу
/// </summary>
public class TrackingEventDto
{
    /// <summary>
    /// Номер ЕН
    /// </summary>
    public string Number { get; set; } = null!;

    /// <summary>
    /// Статус посилки (код)
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Статус посилки (текст)
    /// </summary>
    public string StatusCode { get; set; } = null!;

    /// <summary>
    /// Поточний статус посилки
    /// </summary>
    public string Status_UA { get; set; } = null!; // українською

    public string Status_EN { get; set; } = null!; // англійською

    /// <summary>
    /// Місто відправлення
    /// </summary>
    public string CitySender { get; set; } = null!;

    /// <summary>
    /// Місто отримувача
    /// </summary>
    public string CityRecipient { get; set; } = null!;

    /// <summary>
    /// Відділення відправлення
    /// </summary>
    public string? WarehouseSender { get; set; }

    /// <summary>
    /// Відділення отримувача
    /// </summary>
    public string? WarehouseRecipient { get; set; }

    /// <summary>
    /// Дата очікуваної доставки
    /// </summary>
    public string? DatePayedKeeping { get; set; }

    /// <summary>
    /// Отримувач
    /// </summary>
    public string RecipientFullName { get; set; } = null!;

    /// <summary>
    /// Вага фактична
    /// </summary>
    public decimal? ActualWeight { get; set; }

    /// <summary>
    /// Вартість доставки
    /// </summary>
    public decimal? DocumentCost { get; set; }

    /// <summary>
    /// Зворотня доставка (сума)
    /// </summary>
    public decimal? AmountToPay { get; set; }

    /// <summary>
    /// Сума накладеного платежу
    /// </summary>
    public decimal? AnnouncedPrice { get; set; }

    /// <summary>
    /// Історія статусів
    /// </summary>
    public List<StatusHistoryItem>? StatusHistory { get; set; }
}

/// <summary>
/// Історія зміни статусів посилки
/// </summary>
public class StatusHistoryItem
{
    public string Status { get; set; } = null!;
    public string StatusCode { get; set; } = null!;
    public string DateCreated { get; set; } = null!;
}