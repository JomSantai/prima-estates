using System.ComponentModel.DataAnnotations;

namespace PrimaEstates.Models;

public enum ListingType { Sale = 0, Rent = 1 }
public enum PropertyType { Condominium = 0, Apartment = 1, Terrace = 2, SemiD = 3, Bungalow = 4, ShopLot = 5, Land = 6 }
public enum PropertyStatus { Available = 0, Sold = 1, Rented = 2, Hidden = 3 }

public class Property
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Title { get; set; } = "";

    [StringLength(4000)]
    public string Description { get; set; } = "";

    public ListingType ListingType { get; set; }
    public PropertyType Type { get; set; }
    public PropertyStatus Status { get; set; } = PropertyStatus.Available;

    [Range(0, 100_000_000)]
    public decimal Price { get; set; }

    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
    public int CarParks { get; set; }
    public int BuiltUpSqft { get; set; }

    [Required, StringLength(80)]
    public string City { get; set; } = "";

    [StringLength(80)]
    public string State { get; set; } = "Selangor";

    [StringLength(250)]
    public string Address { get; set; } = "";

    [StringLength(500)]
    public string CoverImageUrl { get; set; } = "";

    public bool IsFeatured { get; set; }

    public int? AgentId { get; set; }
    public Agent? Agent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<PropertyImage> Images { get; set; } = new();
    public List<Enquiry> Enquiries { get; set; } = new();

    public string PriceDisplay =>
        ListingType == ListingType.Rent
            ? $"RM {Price:N0} /mo"
            : $"RM {Price:N0}";

    public string TypeDisplay => Type switch
    {
        PropertyType.SemiD => "Semi-D",
        PropertyType.ShopLot => "Shop Lot",
        _ => Type.ToString()
    };
}

public class PropertyImage
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public Property? Property { get; set; }

    [Required, StringLength(500)]
    public string Url { get; set; } = "";

    public int SortOrder { get; set; }
}

public class Agent
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = "";

    [StringLength(30)]
    public string Phone { get; set; } = "";

    [StringLength(120), EmailAddress]
    public string Email { get; set; } = "";

    [StringLength(50)]
    public string RenLicense { get; set; } = "";

    [StringLength(500)]
    public string PhotoUrl { get; set; } = "";

    public List<Property> Properties { get; set; } = new();
}

public class Enquiry
{
    public int Id { get; set; }

    public int? PropertyId { get; set; }
    public Property? Property { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = "";

    [Required, StringLength(120), EmailAddress]
    public string Email { get; set; } = "";

    [StringLength(30)]
    public string Phone { get; set; } = "";

    [Required, StringLength(2000)]
    public string Message { get; set; } = "";

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AdminUser
{
    public int Id { get; set; }

    [Required, StringLength(60)]
    public string Username { get; set; } = "";

    [Required]
    public string PasswordHash { get; set; } = "";

    [StringLength(100)]
    public string DisplayName { get; set; } = "";
}
