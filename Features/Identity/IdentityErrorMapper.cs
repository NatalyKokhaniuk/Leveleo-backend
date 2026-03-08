using Microsoft.AspNetCore.Identity;

namespace LeveLEO.Features.Identity;

public static class IdentityErrorMapper
{
    public static IReadOnlyCollection<string> Map(IEnumerable<IdentityError> errors)
    {
        var result = new HashSet<string>();

        foreach (var error in errors)
        {
            switch (error.Code)
            {
                case "DuplicateEmail":
                    result.Add("EMAIL_ALREADY_EXISTS");
                    break;

                case "PasswordTooShort":
                    result.Add("PASSWORD_TOO_SHORT");
                    break;

                case "PasswordRequiresDigit":
                    result.Add("PASSWORD_REQUIRES_DIGIT");
                    break;

                case "PasswordRequiresUpper":
                    result.Add("PASSWORD_REQUIRES_UPPERCASE");
                    break;

                case "PasswordRequiresLower":
                    result.Add("PASSWORD_REQUIRES_LOWERCASE");
                    break;

                case "PasswordRequiresNonAlphanumeric":
                    result.Add("PASSWORD_REQUIRES_SYMBOL");
                    break;

                default:
                    result.Add("UNKNOWN_VALIDATION_ERROR");
                    break;
            }
        }

        return result;
    }
}


