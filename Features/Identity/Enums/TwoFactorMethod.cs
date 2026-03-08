using System.Text.Json.Serialization;

namespace LeveLEO.Features.Identity.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TwoFactorMethod
{
    None = 0,
    Email = 1,
    Sms = 2,
    Totp = 3
}
