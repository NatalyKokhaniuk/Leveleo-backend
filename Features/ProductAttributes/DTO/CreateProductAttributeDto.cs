using System;
using System.Collections.Generic;
using LeveLEO.Features.ProductAttributes.Models;

namespace LeveLEO.Features.ProductAttributes.DTO
{
    public class CreateProductAttributeDto
    {
        public Guid AttributeGroupId { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public AttributeType Type { get; set; }

        public string? Unit { get; set; }

        public bool IsFilterable { get; set; } = false;

        public bool IsComparable { get; set; } = false;

        public List<CreateProductAttributeTranslationDto>? Translations { get; set; }
    }
}