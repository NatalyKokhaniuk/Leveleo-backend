using LeveLEO.Features.Identity.Enums;
using System.ComponentModel.DataAnnotations;

namespace LeveLEO.Features.Identity.DTO;

public class InitiateTwoFactorRequestDto

{
    [Required]
    public TwoFactorMethod Method { get; init; }

    
}
