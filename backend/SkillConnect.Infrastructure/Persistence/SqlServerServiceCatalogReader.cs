using Microsoft.EntityFrameworkCore;
using SkillConnect.Application.Services;
using SkillConnect.Domain.Services;

namespace SkillConnect.Infrastructure.Persistence;

public sealed class SqlServerServiceCatalogReader(SkillConnectDbContext dbContext)
    : IServiceCatalogReader
{
    public Task<ServiceListing?> FindPublishedAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.ServiceListings
            .AsNoTracking()
            .SingleOrDefaultAsync(service => service.Id == id && service.IsPublished, cancellationToken);
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
        return await query
            .OrderBy(service => service.Name)
            .ThenBy(service => service.Id)
            .Select(service => new ServiceSummary(service.Id, service.Name, service.Category,
                availableSlots.Where(slot => slot.ServiceId == service.Id)
                    .OrderBy(slot => slot.StartsAt)
                    .Select(slot => (DateTimeOffset?)slot.StartsAt)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }
}
