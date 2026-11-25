using SkillConnect.Domain.Services;

namespace SkillConnect.Application.Services;

public interface IProviderServiceStore
{
    Task AddAsync(ServiceListing listing, CancellationToken cancellationToken);
    Task<IReadOnlyList<ServiceListing>> ListOwnedAsync(Guid providerId, CancellationToken cancellationToken);
    Task<ServiceListing?> FindOwnedAsync(Guid providerId, Guid id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
