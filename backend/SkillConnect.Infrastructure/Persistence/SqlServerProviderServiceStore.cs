using SkillConnect.Application.Services;
using SkillConnect.Domain.Services;

namespace SkillConnect.Infrastructure.Persistence;

public sealed class SqlServerProviderServiceStore(SkillConnectDbContext dbContext) : IProviderServiceStore
{
    public async Task AddAsync(ServiceListing listing, CancellationToken cancellationToken)
    {
        dbContext.ServiceListings.Add(listing);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
