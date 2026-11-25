using SkillConnect.Domain.Services;

namespace SkillConnect.Application.Services;

public interface IProviderServiceStore
{
    Task AddAsync(ServiceListing listing, CancellationToken cancellationToken);
}
