namespace LeveLEO.Infrastructure.Email;

public class EmailTemplateService(IWebHostEnvironment env) : IEmailTemplateService
{
    public string TwoFactorCode(string code)
    {
        var path = Path.Combine(
            env.ContentRootPath,
            "Infrastructure",
            "Email",
            "Templates",
            "TwoFactorCode.html"
        );

        var html = File.ReadAllText(path);
        return html.Replace("{{CODE}}", code);
    }

    string IEmailTemplateService.GetPasswordResetEmail(string resetLink)
    {
        var path = Path.Combine(
        env.ContentRootPath,
        "Infrastructure",
        "Email",
        "Templates",
        "ResetPassword.html"
    );

        var html = File.ReadAllText(path);
        return html.Replace("{{RESET_LINK}}", resetLink);
    }

    string IEmailTemplateService.GetRegistrationConfirmationEmail(string confirmationLink)
    {
        var path = Path.Combine(
        env.ContentRootPath,
        "Infrastructure",
        "Email",
        "Templates",
        "ConfirmEmail.html"
    );

        var html = File.ReadAllText(path);
        return html.Replace("{{CONFIRMATION_LINK}}", confirmationLink);
    }
}