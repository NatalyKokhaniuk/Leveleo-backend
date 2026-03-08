namespace LeveLEO.Infrastructure.Delivery.DTO;

/// <summary>
/// Інформація про експрес-накладну (ЕН)
/// </summary>
public class InternetDocumentDto
{
    /// <summary>
    /// Унікальний ідентифікатор накладної (Ref)
    /// </summary>
    public string Ref { get; set; } = null!;

    /// <summary>
    /// Номер ЕН для трекінгу (20620000000000)
    /// </summary>
    public string IntDocNumber { get; set; } = null!;

    /// <summary>
    /// Вартість доставки (грн)
    /// </summary>
    public decimal CostOnSite { get; set; }

    /// <summary>
    /// Дата створення
    /// </summary>
    public string DateTime { get; set; } = null!;

    /// <summary>
    /// Вага посилки
    /// </summary>
    public decimal Weight { get; set; }

    /// <summary>
    /// Опис вантажу
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// Оціночна вартість
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// Місто відправника
    /// </summary>
    public string CitySender { get; set; } = null!;

    /// <summary>
    /// Місто отримувача
    /// </summary>
    public string CityRecipient { get; set; } = null!;

    /// <summary>
    /// Ім'я відправника
    /// </summary>
    public string SenderDescription { get; set; } = null!;

    /// <summary>
    /// Ім'я отримувача
    /// </summary>
    public string RecipientDescription { get; set; } = null!;

    /// <summary>
    /// Телефон отримувача
    /// </summary>
    public string RecipientsPhone { get; set; } = null!;

    /// <summary>
    /// Тип послуги
    /// </summary>
    public string ServiceType { get; set; } = null!;

    /// <summary>
    /// Статус накладної
    /// </summary>
    public string? StateName { get; set; }

    /// <summary>
    /// Орієнтовна дата доставки
    /// </summary>
    public string? EstimatedDeliveryDate { get; set; }
}