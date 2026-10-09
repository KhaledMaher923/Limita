using Limita.Domain.Common;

namespace Limita.Domain.Entities;

public sealed class Branch : Entity
{
    private Branch() { }

    public string Name { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    public bool IsActive { get; private set; }

    public static Branch Create(string name, string address, string city, string? phoneNumber,
        decimal latitude, decimal longitude) => new()
    {
        Name = Guard.NotBlank(name, "Name", 100),
        Address = Guard.NotBlank(address, "Address", 250),
        City = Guard.NotBlank(city, "City", 100),
        PhoneNumber = Guard.Optional(phoneNumber, "Phone number", 20),
        Latitude = latitude,
        Longitude = longitude,
        IsActive = true
    };
}
