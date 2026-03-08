namespace LeveLEO.Infrastructure.Email;

public interface IEmailTemplateService
{
    string TwoFactorCode(string code);
    string GetRegistrationConfirmationEmail(string confirmationLink);

    string GetPasswordResetEmail(string resetLink);

}
