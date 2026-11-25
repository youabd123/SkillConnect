using SkillConnect.Domain.Services;

namespace SkillConnect.Application.Services;

public interface IServiceCatalogReader
{
    Task<ServiceListing?> FindPublishedAsync(Guid id, CancellationToken cancellationToken);

    // A null category includes all published listings.
    Task<IReadOnlyList<ServiceListing>> ListPublishedAsync(
        string? category,
        CancellationToken cancellationToken);
}
