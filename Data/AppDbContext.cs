using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PrimaEstates.Models;

namespace PrimaEstates.Data;

public class AppDbContext : DbContext, IDataProtectionKeyContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<Enquiry> Enquiries => Set<Enquiry>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    // Persists ASP.NET Data Protection keys so logins/antiforgery tokens
    // survive container restarts and redeploys.
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Property>()
          .HasOne(p => p.Agent)
          .WithMany(a => a.Properties)
          .HasForeignKey(p => p.AgentId)
          .OnDelete(DeleteBehavior.SetNull);

        mb.Entity<PropertyImage>()
          .HasOne(i => i.Property)
          .WithMany(p => p.Images)
          .HasForeignKey(i => i.PropertyId)
          .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<Enquiry>()
          .HasOne(e => e.Property)
          .WithMany(p => p.Enquiries)
          .HasForeignKey(e => e.PropertyId)
          .OnDelete(DeleteBehavior.SetNull);

        mb.Entity<AdminUser>().HasIndex(u => u.Username).IsUnique();
    }
}
