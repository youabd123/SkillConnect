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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var listing = modelBuilder.Entity<ServiceListing>();
        listing.HasKey(service => service.Id);
        listing.Property(service => service.Id).ValueGeneratedNever();
        listing.Property(service => service.ProviderId);
    }
}
