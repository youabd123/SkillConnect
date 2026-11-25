using Microsoft.EntityFrameworkCore;
using SkillConnect.Domain.Services;

namespace SkillConnect.Infrastructure.Persistence;

public sealed class SkillConnectDbContext(DbContextOptions<SkillConnectDbContext> options)
    : DbContext(options)
{
    public DbSet<ServiceListing> ServiceListings => Set<ServiceListing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var listing = modelBuilder.Entity<ServiceListing>();
        listing.HasKey(service => service.Id);
        listing.Property(service => service.Id).ValueGeneratedNever();
        listing.Property(service => service.ProviderId);
    }
}
