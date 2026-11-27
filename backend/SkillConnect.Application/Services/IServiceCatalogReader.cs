namespace SkillConnect.Application.Services;

public interface IServiceCatalogReader
{
    Task<ServiceDetails?> FindPublishedAsync(Guid id, CancellationToken cancellationToken);
    Task<PublicProviderProfile?> FindProviderAsync(Guid id, DateTimeOffset now, CancellationToken cancellationToken);

    // A null category includes all published listings.
    Task<IReadOnlyList<ServiceSummary>> ListPublishedAsync(
        string? category,
        string? search,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
