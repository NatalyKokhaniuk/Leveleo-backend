using LeveLEO.Features.Shipping.DTO;
using LeveLEO.Features.Shipping.Models;

namespace LeveLEO.Features.Shipping.Services;

public interface IAddressService
{
    // CRUD для адрес користувача
    Task<AddressResponseDto> CreateAddressAsync(string userId, CreateAddressDto dto);

    Task<AddressResponseDto> UpdateAddressAsync(string userId, Guid addressId, UpdateAddressDto dto);

    Task DeleteAddressAsync(string userId, Guid addressId);

    Task<AddressResponseDto> GetAddressByIdAsync(Guid addressId);

    Task<List<AddressResponseDto>> GetUserAddressesAsync(string userId);

    // Встановити адресу за замовчуванням (опційно)
    Task<AddressResponseDto> SetDefaultAddressAsync(string userId, Guid addressId);

    AddressResponseDto MapToDto(Address address);
}