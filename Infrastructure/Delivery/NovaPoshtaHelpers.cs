namespace LeveLEO.Infrastructure.Delivery;

public static class NovaPoshtaHelpers
{
    /// <summary>
    /// Формат дати для НП API: dd.MM.yyyy
    /// </summary>
    public static string FormatDate(DateTimeOffset date)
    {
        return date.ToString("dd.MM.yyyy");
    }

    /// <summary>
    /// Формат дати для НП API: dd.MM.yyyy
    /// </summary>
    public static string FormatDate(DateTime date)
    {
        return date.ToString("dd.MM.yyyy");
    }

    /// <summary>
    /// Валідація телефону (+380XXXXXXXXX)
    /// </summary>
    public static string FormatPhone(string phone)
    {

        var digitsOnly = new string(phone.Where(char.IsDigit).ToArray());

        if (digitsOnly.StartsWith("380") && digitsOnly.Length == 12)
        {
            return "+" + digitsOnly;
        }

        if (digitsOnly.StartsWith("0") && digitsOnly.Length == 10)
        {
            return "+380" + digitsOnly.Substring(1);
        }

        throw new ArgumentException("Invalid phone number format. Expected: +380XXXXXXXXX");
    }
}
