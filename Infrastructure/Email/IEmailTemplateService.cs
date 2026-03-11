namespace LeveLEO.Infrastructure.Email;

public interface IEmailTemplateService
{
    string TwoFactorCode(string code);

    string GetRegistrationConfirmationEmail(string confirmationLink);

    string GetPasswordResetEmail(string resetLink);

    /// <summary>
    /// Отримати темплейт за назвою з підстановкою плейсхолдерів
    /// </summary>
    Task<string> GetTemplateAsync(string templateName, Dictionary<string, string> replacements);
}