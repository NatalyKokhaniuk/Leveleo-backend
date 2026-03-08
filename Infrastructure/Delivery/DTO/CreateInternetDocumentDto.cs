namespace LeveLEO.Infrastructure.Delivery.DTO;

/// <summary>
/// DTO для створення експрес-накладної (ЕН) в Новій Пошті
/// </summary>
public class CreateInternetDocumentDto
{
    // === Обов'язкові поля ===

    /// <summary>
    /// Тип платник: Sender (відправник), Recipient (отримувач)
    /// </summary>
    public string PayerType { get; set; } = "Sender";

    /// <summary>
    /// Форма оплати: Cash (готівка), NonCash (безготівка)
    /// </summary>
    public string PaymentMethod { get; set; } = "NonCash";

    /// <summary>
    /// Дата відправки (формат: dd.MM.yyyy)
    /// </summary>
    public string DateTime { get; set; } = null!;

    /// <summary>
    /// Тип доставки вантажу: Warehouse (на відділення), Doors (адресна)
    /// </summary>
    public string CargoType { get; set; } = "Cargo";

    /// <summary>
    /// Вага посилки (кг), мінімум 0.1
    /// </summary>
    public decimal Weight { get; set; }

    /// <summary>
    /// Тип послуги: WarehouseWarehouse, WarehouseDoors, DoorsWarehouse, DoorsDoors
    /// </summary>
    public string ServiceType { get; set; } = "WarehouseWarehouse";

    /// <summary>
    /// Кількість місць (посилок)
    /// </summary>
    public int SeatsAmount { get; set; } = 1;

    /// <summary>
    /// Опис вантажу
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// Оціночна вартість (грн)
    /// </summary>
    public decimal Cost { get; set; }

    // === Відправник ===

    /// <summary>
    /// Ref міста відправника
    /// </summary>
    public string CitySender { get; set; } = null!;

    /// <summary>
    /// Ім'я відправника
    /// </summary>
    public string SenderName { get; set; } = null!;

    /// <summary>
    /// Телефон відправника (+380XXXXXXXXX)
    /// </summary>
    public string SendersPhone { get; set; } = null!;

    /// <summary>
    /// Ref відділення відправника (для WarehouseWarehouse, WarehouseDoors)
    /// </summary>
    public string? SenderWarehouse { get; set; }

    /// <summary>
    /// Адреса відправника (для DoorsWarehouse, DoorsDoors)
    /// </summary>
    public string? SenderAddress { get; set; }

    // === Отримувач ===

    /// <summary>
    /// Ref міста отримувача
    /// </summary>
    public string RecipientCityRef { get; set; } = null!;

    /// <summary>
    /// Ім'я отримувача
    /// </summary>
    public string RecipientName { get; set; } = null!;

    /// <summary>
    /// Прізвище отримувача
    /// </summary>
    public string RecipientSurname { get; set; } = null!;

    /// <summary>
    /// По батькові отримувача
    /// </summary>
    public string? RecipientMiddleName { get; set; }

    /// <summary>
    /// Телефон отримувача (+380XXXXXXXXX)
    /// </summary>
    public string RecipientsPhone { get; set; } = null!;

    /// <summary>
    /// Ref відділення отримувача (для WarehouseWarehouse, DoorsWarehouse)
    /// </summary>
    public string? RecipientWarehouse { get; set; }

    /// <summary>
    /// Адреса отримувача (для WarehouseDoors, DoorsDoors)
    /// </summary>
    public string? RecipientAddress { get; set; }

    // === Додаткові поля ===

    /// <summary>
    /// Зворотня доставка: null, Money (грошовий переказ), Documents (документи), CargoReturn (посилка)
    /// </summary>
    public string? BackwardDeliveryType { get; set; }

    /// <summary>
    /// Сума зворотної доставки (для Money)
    /// </summary>
    public decimal? BackwardDeliveryCost { get; set; }

    /// <summary>
    /// Оголошена вартість (для страхування)
    /// </summary>
    public decimal? AfterpaymentOnGoodsCost { get; set; }
}