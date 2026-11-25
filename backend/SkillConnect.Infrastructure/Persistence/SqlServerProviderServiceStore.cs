using Microsoft.EntityFrameworkCore;
using SkillConnect.Application.Services;
using SkillConnect.Domain.Services;

namespace SkillConnect.Infrastructure.Persistence;

public sealed class SqlServerProviderServiceStore(SkillConnectDbContext dbContext) : IProviderServiceStore
{
    public Task AddAsync(ServiceListing listing, CancellationToken cancellationToken)
    {
        dbContext.ServiceListings.Add(listing);
        return SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceListing>> ListOwnedAsync(Guid providerId, CancellationToken cancellationToken)
    {
        return await dbContext.ServiceListings
            .AsNoTracking()
            .Where(service => service.ProviderId == providerId)
            .OrderBy(service => service.Name)
            .ThenBy(service => service.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<ServiceListing?> FindOwnedAsync(Guid providerId, Guid id, CancellationToken cancellationToken) =>
        dbContext.ServiceListings.SingleOrDefaultAsync(
            service => service.ProviderId == providerId && service.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
