using SkillConnect.Domain.Services;

namespace SkillConnect.Application.Services;

public interface IServiceCatalogReader
{
    // A null category includes all published listings.
    Task<IReadOnlyList<ServiceListing>> ListPublishedAsync(
        string? category,
        CancellationToken cancellationToken);
}
