using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SkillConnect.Domain.Services;
using SkillConnect.Infrastructure.Identity;

namespace SkillConnect.Infrastructure.Persistence;

public sealed class SkillConnectDbContext(DbContextOptions<SkillConnectDbContext> options)
    : IdentityDbContext<SkillConnectUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<ServiceListing> ServiceListings => Set<ServiceListing>();
    public DbSet<AvailabilitySlot> AvailabilitySlots => Set<AvailabilitySlot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var listing = modelBuilder.Entity<ServiceListing>();
        listing.HasKey(service => service.Id);
        listing.Property(service => service.Id).ValueGeneratedNever();
        listing.Property(service => service.ProviderId);

        var availability = modelBuilder.Entity<AvailabilitySlot>();
        availability.ToTable("AvailabilitySlots", table =>
            table.HasCheckConstraint("CK_AvailabilitySlots_EndsAfterStart", "[EndsAt] > [StartsAt]"));
        availability.HasKey(slot => slot.Id);
        availability.Property(slot => slot.Id).ValueGeneratedNever();
        availability.Property(slot => slot.ServiceId);
        availability.Property(slot => slot.StartsAt);
        availability.Property(slot => slot.EndsAt);
        availability.HasOne<ServiceListing>().WithMany().HasForeignKey(slot => slot.ServiceId);
        availability.HasIndex(slot => new { slot.ServiceId, slot.StartsAt });
    }
}
