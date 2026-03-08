namespace LeveLEO.Infrastructure.SMS;

public class SmsSender(IConfiguration configuration) : ISmsSender
{
    private static readonly HttpClient httpClient = new();

    public async Task SendSmsAsync(string phoneNumber, string message)
    {
        var smsSection = configuration.GetSection("SMS:Smtp");
        string url = smsSection["Url"] ?? "https://im.smsclub/sms/send";
        string smsToken = smsSection["Token"] ?? "";
        string addr = smsSection["Addr"] ?? "AUTO";
        var payload = new
        {
            phone = new string[] { phoneNumber },
            message,
            src_addr = addr
        };
        var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", smsToken);
        try
        {
            HttpResponseMessage responseMessage = await httpClient.PostAsync(url, content);
            responseMessage.EnsureSuccessStatusCode();

            string responseBody = await responseMessage.Content.ReadAsStringAsync();
            Console.WriteLine($"SMS відправлено на {phoneNumber} з текстом: {message}. Відповідь сервера: {responseBody}");
        }
        catch (HttpIOException ex)
        {
            Console.WriteLine($"Помилка відправки SMS на {phoneNumber} з текстом: {message}. Помилка: {ex.Message}");
        }
    }
}