namespace LeveLEO.Features.Identity.DTO;

public class ConfirmTwoFactorSetupResponseDto
{
    public bool Success { get; set; } = false!; 
    public string Message { get; set; } = null!; 
    public string Method{get; set; } = null!;
}
