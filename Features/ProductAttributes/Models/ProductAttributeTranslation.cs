using LeveLEO.Infrastructure.Translation.Models;
using System.ComponentModel.DataAnnotations;

namespace LeveLEO.Features.ProductAttributes.Models;

public class ProductAttributeTranslation : ITranslation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ProductAttributeId { get; set; }

    [Required]
    [MaxLength(5)]
    public string LanguageCode { get; set; } = null!;

    [Required]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public ProductAttribute ProductAttribute { get; set; } = null!;
}