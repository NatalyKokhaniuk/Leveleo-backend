using LeveLEO.Infrastructure.Translation.Models;
using System.ComponentModel.DataAnnotations;

namespace LeveLEO.Features.Promotions.Models;

public class PromotionTranslation : ITranslation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid PromotionId { get; set; }

    [Required]
    [MaxLength(5)]
    public string LanguageCode { get; set; } = null!;

    [Required]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public Promotion Promotion { get; set; } = null!;
}