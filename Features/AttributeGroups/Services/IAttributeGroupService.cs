using LeveLEO.Features.AttributeGroups.DTO;

namespace LeveLEO.Features.AttributeGroups.Services;

public interface IAttributeGroupService
{
    Task<AttributeGroupResponseDto> CreateAsync(CreateAttributeGroupDto dto);

    Task<AttributeGroupResponseDto> GetByIdAsync(Guid groupId);

    Task<AttributeGroupResponseDto> GetBySlugAsync(string slug);

    Task<List<AttributeGroupResponseDto>> GetAllAsync();

    Task<List<AttributeGroupResponseDto>> SearchAsync(string query);

    Task<AttributeGroupResponseDto> UpdateAsync(Guid groupId, UpdateAttributeGroupDto dto);

    Task DeleteAsync(Guid groupId);

    Task<AttributeGroupTranslationResponseDto> AddTranslationAsync(Guid groupId, CreateAttributeGroupTranslationDto dto);

    Task<AttributeGroupTranslationResponseDto> UpdateTranslationAsync(Guid groupId, CreateAttributeGroupTranslationDto dto);

    Task DeleteTranslationAsync(Guid groupId, string languageCode);

    Task<List<AttributeGroupTranslationResponseDto>> GetTranslationsByGroupIdAsync(Guid groupId);

    Task<AttributeGroupTranslationResponseDto> GetTranslationAsync(Guid groupId, string languageCode);
}