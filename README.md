# Prima Estates — ASP.NET Core Real Estate Website

A complete real estate website with a public listing site and a secure admin dashboard,
built for the Malaysian market (RM pricing, Klang Valley sample data, REN agent licences).

## Stack

- ASP.NET Core 8 MVC (Razor views)
- Entity Framework Core 8 + SQLite (zero-config; swap the connection string for SQL Server/PostgreSQL later)
- Cookie authentication for the admin area
- No frontend build step — plain CSS with Google Fonts

## Features

**Public site**
- Homepage with hero search, featured & latest listings
- Property listings with filters: keyword, buy/rent, type, area, price range, bedrooms + pagination
- Property detail page with image gallery, specs, agent card, and enquiry form
- Agents page and contact page (enquiries saved to the database)

**Admin dashboard** (`/account/login`)
- Stats overview: total/active listings, sold/rented, unread enquiries
- Full property CRUD with cover image + gallery upload (or paste image URLs)
- Agent CRUD with photo upload
- Enquiry inbox with read/unread and delete
- Anti-forgery tokens on all forms; uploads restricted to jpg/png/webp, max 5 MB

## Getting started

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
cd PrimaEstates
dotnet run
```

Open the URL shown in the console (e.g. https://localhost:5001).
The SQLite database `primaestates.db` is created and seeded automatically on first run
(8 sample properties, 3 agents, 1 admin user).

## Default admin login

- URL: `/account/login`
- Username: `admin`
- Password: `Admin@123`

**Change this before going live.** To change it, delete the row in `AdminUsers`
(or delete `primaestates.db` to reseed) after editing the password in `Data/SeedData.cs`.

## Project structure

```
PrimaEstates/
├── Program.cs                  # App startup, auth, EF, seeding
├── appsettings.json            # Connection string
├── Models/Entities.cs          # Property, Agent, Enquiry, PropertyImage, AdminUser
├── Data/AppDbContext.cs        # EF Core context
├── Data/SeedData.cs            # Seed admin + sample Klang Valley data
├── Controllers/
│   ├── HomeController.cs       # Home, agents, contact
│   ├── PropertiesController.cs # Listings, filters, details, enquiries
│   ├── AccountController.cs    # Admin login/logout (cookie auth)
│   └── AdminController.cs      # Dashboard + all CRUD ([Authorize])
├── Views/                      # Razor views (public + admin layouts)
└── wwwroot/
    ├── css/site.css            # Public site styles
    ├── css/admin.css           # Admin styles
    └── uploads/                # Uploaded images land here
```

## Deploying

- **Windows / IIS or Linux + Nginx:** `dotnet publish -c Release` and host the output.
- **Railway / Docker:** add a Dockerfile based on `mcr.microsoft.com/dotnet/aspnet:8.0`.
- For production, switch SQLite to SQL Server or PostgreSQL by changing the provider
  package and `UseSqlite` → `UseSqlServer`/`UseNpgsql` in `Program.cs`.
- Uploaded images are stored on disk in `wwwroot/uploads` — on ephemeral hosts (Railway),
  move uploads to Cloudinary/S3 or mount a volume.
