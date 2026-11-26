using Microsoft.EntityFrameworkCore;
using SkillConnect.Application.Services;
using SkillConnect.Infrastructure.Identity;

namespace SkillConnect.Infrastructure.Persistence;

public sealed class SqlServerServiceCatalogReader(SkillConnectDbContext dbContext)
    : IServiceCatalogReader
{
    public Task<ServiceDetails?> FindPublishedAsync(Guid id, CancellationToken cancellationToken)
    {
        var providers = dbContext.Users.Where(user => user.AccountRole == AccountRole.Provider);
        return dbContext.ServiceListings
            .AsNoTracking()
            .Where(service => service.Id == id && service.IsPublished)
            .Select(service => new ServiceDetails(service.Id, service.Name, service.Description, service.Category,
                providers.Where(provider => provider.Id == service.ProviderId).Select(provider => provider.BusinessName).FirstOrDefault()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceSummary>> ListPublishedAsync(
        string? category,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ServiceListings
            .AsNoTracking()
            .Where(service => service.IsPublished);

        if (category is not null)
        {
            query = query.Where(service => service.Category == category);
        }

        var availableSlots = dbContext.GetUpcomingAvailableSlots(now);
        var providers = dbContext.Users.Where(user => user.AccountRole == AccountRole.Provider);
        return await query
            .OrderBy(service => service.Name)
            .ThenBy(service => service.Id)
            .Select(service => new ServiceSummary(service.Id, service.Name, service.Category,
                availableSlots.Where(slot => slot.ServiceId == service.Id)
                    .OrderBy(slot => slot.StartsAt)
                    .Select(slot => (DateTimeOffset?)slot.StartsAt)
                    .FirstOrDefault(),
                providers.Where(provider => provider.Id == service.ProviderId).Select(provider => provider.BusinessName).FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }
}
