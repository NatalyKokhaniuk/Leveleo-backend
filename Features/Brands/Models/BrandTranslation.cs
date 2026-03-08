using LeveLEO.Infrastructure.Translation.Models;
using System.ComponentModel.DataAnnotations;

namespace LeveLEO.Features.Brands.Models;

public class BrandTranslation : ITranslation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid BrandId { get; set; }

    [Required]
    [MaxLength(5)]
    public string LanguageCode { get; set; } = null!;

    [Required]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    // Навігаційна властивість
    public Brand Brand { get; set; } = null!;
}