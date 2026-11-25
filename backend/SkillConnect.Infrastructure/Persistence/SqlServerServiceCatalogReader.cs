using Microsoft.EntityFrameworkCore;
using SkillConnect.Application.Services;
using SkillConnect.Domain.Services;

namespace SkillConnect.Infrastructure.Persistence;

public sealed class SqlServerServiceCatalogReader(SkillConnectDbContext dbContext)
    : IServiceCatalogReader
{
    public async Task<IReadOnlyList<ServiceListing>> ListPublishedAsync(
        string? category,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ServiceListings
            .AsNoTracking()
            .Where(service => service.IsPublished);

        if (category is not null)
        {
            query = query.Where(service => service.Category == category);
        }

        return await query
            .OrderBy(service => service.Name)
            .ThenBy(service => service.Id)
            .ToListAsync(cancellationToken);
    }
}
