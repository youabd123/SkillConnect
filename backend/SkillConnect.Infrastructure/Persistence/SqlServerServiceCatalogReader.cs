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
                    provider == null ? null : provider.BusinessName, provider == null ? null : provider.Id,
                    service.DeliveryMode, service.City))
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

        var services = await ListSummariesAsync(PublishedServices.Where(service => service.ProviderId == id), null, now, cancellationToken);
        return new PublicProviderProfile(id, provider.BusinessName!, provider.BusinessDescription, services);
    }

    public Task<IReadOnlyList<ServiceSummary>> ListPublishedAsync(
        ServiceCatalogFilter filter,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var query = PublishedServices;

        if (filter.Category is { } category)
        {
            query = query.Where(service => service.Category == category);
        }

        if (filter.City is { } city)
        {
            query = query.Where(service => service.City == city);
        }

        if (filter.Remote)
        {
            query = query.Where(service =>
                service.DeliveryMode == ServiceDeliveryMode.Remote || service.DeliveryMode == ServiceDeliveryMode.Both);
        }

        return ListSummariesAsync(query, filter.Search, now, cancellationToken);
    }

    private async Task<IReadOnlyList<ServiceSummary>> ListSummariesAsync(
        IQueryable<ServiceListing> services, string? search, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var availableSlots = dbContext.GetUpcomingAvailableSlots(now);
        var listings = from service in services
                       join provider in PublicProviders on service.ProviderId equals provider.Id into providers
                       from provider in providers.DefaultIfEmpty()
                       select new { Service = service, Provider = provider };
        if (search is not null)
        {
            listings = listings.Where(item => item.Service.Name.Contains(search) ||
                (item.Provider != null && item.Provider.BusinessName!.Contains(search)));
        }

        return await listings.OrderBy(item => item.Service.Name).ThenBy(item => item.Service.Id)
            .Select(item => new ServiceSummary(item.Service.Id, item.Service.Name, item.Service.Category,
                          availableSlots.Where(slot => slot.ServiceId == item.Service.Id)
                              .OrderBy(slot => slot.StartsAt)
                              .Select(slot => (DateTimeOffset?)slot.StartsAt)
                              .FirstOrDefault(),
                          item.Provider == null ? null : item.Provider.BusinessName,
                          item.Provider == null ? null : item.Provider.Id, item.Service.DeliveryMode, item.Service.City))
            .ToListAsync(cancellationToken);
    }
}
