using LeveLEO.Features.Shipping.Models;
using LeveLEO.Infrastructure.Common;

namespace LeveLEO.Features.Shipping.DTO;

public class UpdateAddressDto
{
    public Optional<string> FirstName { get; set; }
    public Optional<string> LastName { get; set; }
    public Optional<string> MiddleName { get; set; }
    public Optional<string> PhoneNumber { get; set; }

    public Optional<DeliveryType> DeliveryType { get; set; }

    public Optional<string?> CityRef { get; set; }
    public Optional<string?> CityName { get; set; }
    public Optional<string?> WarehouseRef { get; set; }

    public Optional<string?> StreetRef { get; set; }
    public Optional<string?> Street { get; set; }
    public Optional<string?> House { get; set; }
    public Optional<string?> Flat { get; set; }
    public Optional<string?> Floor { get; set; }

    public Optional<string?> AdditionalInfo { get; set; }
}