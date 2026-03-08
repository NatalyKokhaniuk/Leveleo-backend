namespace LeveLEO.Infrastructure.SMS;

public interface ISmsSender
{
    Task SendSmsAsync(string phoneNumber, string message);
}
