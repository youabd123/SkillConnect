using Microsoft.EntityFrameworkCore;
using SkillConnect.Application.Services;
using SkillConnect.Domain.Services;
using SkillConnect.Infrastructure.Identity;

namespace SkillConnect.Infrastructure.Persistence;

public sealed class SqlServerServiceCatalogReader(SkillConnectDbContext dbContext)
    : IServiceCatalogReader
{
    private IQueryable<SkillConnectUser> PublicProviders => dbContext.Users.AsNoTracking()
        .Where(user => user.AccountRole == AccountRole.Provider && user.EmailConfirmed && user.BusinessName != null);

    private IQueryable<ServiceListing> PublishedServices => dbContext.ServiceListings.AsNoTracking()
        .Where(service => service.IsPublished);

    public Task<ServiceDetails?> FindPublishedAsync(Guid id, CancellationToken cancellationToken)
    {
        return (from service in PublishedServices
                where service.Id == id
                join provider in PublicProviders on service.ProviderId equals provider.Id into providers
                from provider in providers.DefaultIfEmpty()
                select new ServiceDetails(service.Id, service.Name, service.Description, service.Category,
                    provider == null ? null : provider.BusinessName, provider == null ? null : provider.Id))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PublicProviderProfile?> FindProviderAsync(Guid id, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var provider = await PublicProviders.Where(user => user.Id == id)
            .Select(user => new { user.BusinessName, user.BusinessDescription })
            .SingleOrDefaultAsync(cancellationToken);
        if (provider is null)
        {
            return null;
        }

        var services = await ListSummariesAsync(PublishedServices.Where(service => service.ProviderId == id), now, cancellationToken);
        return new PublicProviderProfile(id, provider.BusinessName!, provider.BusinessDescription, services);
    }

    public Task<IReadOnlyList<ServiceSummary>> ListPublishedAsync(
        string? category,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var query = PublishedServices;

        if (category is not null)
        {
            query = query.Where(service => service.Category == category);
        }

        return ListSummariesAsync(query, now, cancellationToken);
    }

    private async Task<IReadOnlyList<ServiceSummary>> ListSummariesAsync(
        IQueryable<ServiceListing> services, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var availableSlots = dbContext.GetUpcomingAvailableSlots(now);
        return await (from service in services
                      join provider in PublicProviders on service.ProviderId equals provider.Id into providers
                      from provider in providers.DefaultIfEmpty()
                      orderby service.Name, service.Id
                      select new ServiceSummary(service.Id, service.Name, service.Category,
                          availableSlots.Where(slot => slot.ServiceId == service.Id)
                              .OrderBy(slot => slot.StartsAt)
                              .Select(slot => (DateTimeOffset?)slot.StartsAt)
                              .FirstOrDefault(),
                          provider == null ? null : provider.BusinessName, provider == null ? null : provider.Id))
            .ToListAsync(cancellationToken);
    }
}
