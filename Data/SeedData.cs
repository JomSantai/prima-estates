using Microsoft.AspNetCore.Identity;
using PrimaEstates.Models;

namespace PrimaEstates.Data;

public static class SeedData
{
    public static void Initialize(AppDbContext db, IConfiguration? config = null)
    {
        // Admin credentials come from environment first, then fall back to defaults for local dev.
        var adminUsername = config?["Admin:Username"]
            ?? Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? "admin";
        var adminPassword = config?["Admin:Password"]
            ?? Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "Admin@123";

        if (!db.AdminUsers.Any())
        {
            var hasher = new PasswordHasher<AdminUser>();
            var admin = new AdminUser { Username = adminUsername, DisplayName = "Administrator" };
            admin.PasswordHash = hasher.HashPassword(admin, adminPassword);
            db.AdminUsers.Add(admin);
        }

        if (!db.Agents.Any())
        {
            db.Agents.AddRange(
                new Agent { Name = "Aisyah Rahman", Phone = "+60 12-345 6789", Email = "aisyah@primaestates.my", RenLicense = "REN 12345", PhotoUrl = "https://i.pravatar.cc/300?img=47" },
                new Agent { Name = "Daniel Lim", Phone = "+60 16-234 5678", Email = "daniel@primaestates.my", RenLicense = "REN 23456", PhotoUrl = "https://i.pravatar.cc/300?img=12" },
                new Agent { Name = "Kavitha Nair", Phone = "+60 17-987 6543", Email = "kavitha@primaestates.my", RenLicense = "REN 34567", PhotoUrl = "https://i.pravatar.cc/300?img=32" }
            );
            db.SaveChanges();
        }

        if (!db.Properties.Any())
        {
            var agents = db.Agents.ToList();
            string Img(string seed) => $"https://picsum.photos/seed/{seed}/900/600";

            var props = new List<Property>
            {
                new() { Title = "Modern 3-Storey Semi-D, Bukit Tinggi", Type = PropertyType.SemiD, ListingType = ListingType.Sale,
                        Price = 1_680_000, Bedrooms = 5, Bathrooms = 6, CarParks = 4, BuiltUpSqft = 3800,
                        City = "Klang", Address = "Jalan Batu Nilam, Bandar Bukit Tinggi", IsFeatured = true,
                        CoverImageUrl = Img("semid1"), AgentId = agents[0].Id,
                        Description = "Fully renovated semi-detached home in the heart of Bukit Tinggi. Open-plan wet and dry kitchen, private lift, solar water heating, and a landscaped garden. Walking distance to AEON Bukit Tinggi and top schools." },
                new() { Title = "Sky Residences Condo, i-City", Type = PropertyType.Condominium, ListingType = ListingType.Sale,
                        Price = 620_000, Bedrooms = 3, Bathrooms = 2, CarParks = 2, BuiltUpSqft = 1150,
                        City = "Shah Alam", Address = "Persiaran Multimedia, Seksyen 7", IsFeatured = true,
                        CoverImageUrl = Img("condo1"), AgentId = agents[1].Id,
                        Description = "High-floor corner unit with unobstructed lake views. Facilities include infinity pool, sky gym, co-working lounge and 3-tier security. Direct link bridge to i-City mall." },
                new() { Title = "Double Storey Terrace, Setia Alam", Type = PropertyType.Terrace, ListingType = ListingType.Sale,
                        Price = 780_000, Bedrooms = 4, Bathrooms = 3, CarParks = 2, BuiltUpSqft = 2000,
                        City = "Setia Alam", Address = "Jalan Setia Impian", IsFeatured = true,
                        CoverImageUrl = Img("terrace1"), AgentId = agents[2].Id,
                        Description = "Freehold, gated & guarded 22x75 terrace facing open green. Extended kitchen, plaster ceiling and autogate. Minutes from Setia City Mall and NKVE access." },
                new() { Title = "Cozy Studio @ Empire City", Type = PropertyType.Apartment, ListingType = ListingType.Rent,
                        Price = 1_500, Bedrooms = 1, Bathrooms = 1, CarParks = 1, BuiltUpSqft = 550,
                        City = "Petaling Jaya", Address = "Jalan PJU 8, Damansara Perdana",
                        CoverImageUrl = Img("studio1"), AgentId = agents[1].Id,
                        Description = "Fully furnished studio with high-speed fibre included. Ideal for young professionals working in Damansara or Mutiara Damansara. Flexible 12-month lease." },
                new() { Title = "Corner Shop Lot, Bandar Botanic", Type = PropertyType.ShopLot, ListingType = ListingType.Rent,
                        Price = 6_800, Bedrooms = 0, Bathrooms = 2, CarParks = 0, BuiltUpSqft = 3200,
                        City = "Klang", Address = "Lorong Batu Nilam 21, Bandar Botanic",
                        CoverImageUrl = Img("shop1"), AgentId = agents[0].Id,
                        Description = "High-visibility corner ground-floor shop with wide frontage, suitable for F&B or clinic. Ample public parking, established neighbourhood catchment." },
                new() { Title = "Luxury Bungalow, Kota Kemuning", Type = PropertyType.Bungalow, ListingType = ListingType.Sale,
                        Price = 3_200_000, Bedrooms = 6, Bathrooms = 7, CarParks = 6, BuiltUpSqft = 6500,
                        City = "Shah Alam", Address = "Jalan Anggerik Vanilla, Kota Kemuning", IsFeatured = true,
                        CoverImageUrl = Img("bungalow1"), AgentId = agents[2].Id,
                        Description = "Resort-style bungalow with private pool, home theatre and guest annexe. Gated precinct with 24-hour patrol. Golf course a short drive away." },
                new() { Title = "Family Condo @ Tropicana Aman", Type = PropertyType.Condominium, ListingType = ListingType.Rent,
                        Price = 2_300, Bedrooms = 3, Bathrooms = 2, CarParks = 2, BuiltUpSqft = 1050,
                        City = "Telok Panglima Garang", Address = "Persiaran Tropicana Aman",
                        CoverImageUrl = Img("condo2"), AgentId = agents[0].Id,
                        Description = "Partially furnished family unit beside 85-acre central park. Kitchen cabinet, aircon in all rooms, and access to lap pool, gym and playground." },
                new() { Title = "Agricultural Land 2.5 Acres, Kapar", Type = PropertyType.Land, ListingType = ListingType.Sale,
                        Price = 1_950_000, Bedrooms = 0, Bathrooms = 0, CarParks = 0, BuiltUpSqft = 108_900,
                        City = "Kapar", Address = "Off Jalan Kapar, Mukim Kapar",
                        CoverImageUrl = Img("land1"), AgentId = agents[1].Id,
                        Description = "Flat, road-fronting agricultural land with conversion potential. Suitable for nursery, storage yard or long-term investment. Clean individual title." },
            };

            db.Properties.AddRange(props);
            db.SaveChanges();

            foreach (var p in props)
            {
                db.PropertyImages.AddRange(
                    new PropertyImage { PropertyId = p.Id, Url = p.CoverImageUrl, SortOrder = 0 },
                    new PropertyImage { PropertyId = p.Id, Url = $"https://picsum.photos/seed/p{p.Id}b/900/600", SortOrder = 1 },
                    new PropertyImage { PropertyId = p.Id, Url = $"https://picsum.photos/seed/p{p.Id}c/900/600", SortOrder = 2 }
                );
            }
        }

        db.SaveChanges();
    }
}
