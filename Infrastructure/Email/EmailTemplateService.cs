namespace LeveLEO.Infrastructure.Email;

public class EmailTemplateService(IWebHostEnvironment env) : IEmailTemplateService
{
    public string TwoFactorCode(string code)
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
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
        AppContext.BaseDirectory,
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
        AppContext.BaseDirectory,  // ← замість env.ContentRootPath
        "Infrastructure",
        "Email",
        "Templates",
        "ConfirmEmail.html"
    );
        Console.WriteLine($"[EMAIL TEMPLATE] BaseDirectory: {AppContext.BaseDirectory}");
        Console.WriteLine($"[EMAIL TEMPLATE] Full path: {path}");
        Console.WriteLine($"[EMAIL TEMPLATE] File exists: {File.Exists(path)}");
        var html = File.ReadAllText(path);
        return html.Replace("{{CONFIRMATION_LINK}}", confirmationLink);
    }

    public async Task<string> GetTemplateAsync(string templateName, Dictionary<string, string> replacements)
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Infrastructure",
            "Email",
            "Templates",
            $"{templateName}.html"
        );

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Email template '{templateName}.html' not found");
        }

        var html = await File.ReadAllTextAsync(path);

        foreach (var replacement in replacements)
        {
            html = html.Replace(replacement.Key, replacement.Value);
        }

        return html;
    }
}