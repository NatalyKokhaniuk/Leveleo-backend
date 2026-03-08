using System.ComponentModel.DataAnnotations;
using LeveLEO.Infrastructure.Translation.Models;

namespace LeveLEO.Features.ProductAttributeValues.Models;

public class ProductAttributeValueTranslation : ITranslation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ProductAttributeValueId { get; set; }

    [Required]
    [MaxLength(5)]
    public string LanguageCode { get; set; } = null!;

    [Required]
    public string Value { get; set; } = null!;

    public ProductAttributeValue ProductAttributeValue { get; set; } = null!;
}