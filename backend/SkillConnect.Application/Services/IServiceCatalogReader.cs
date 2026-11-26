using SkillConnect.Domain.Services;

namespace SkillConnect.Application.Services;

public interface IServiceCatalogReader
{
    Task<ServiceListing?> FindPublishedAsync(Guid id, CancellationToken cancellationToken);

    // A null category includes all published listings.
    Task<IReadOnlyList<ServiceSummary>> ListPublishedAsync(
        string? category,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
