namespace SkillConnect.Application.Services;

public sealed class ListPublishedServices(IServiceCatalogReader catalogReader, TimeProvider clock)
{
    public Task<IReadOnlyList<ServiceSummary>> ExecuteAsync(
        string? category,
        string? search,
        CancellationToken cancellationToken)
    {
        var categoryFilter = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        var searchTerm = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        return catalogReader.ListPublishedAsync(categoryFilter, searchTerm, clock.GetUtcNow(), cancellationToken);
    }
}

public sealed record ServiceSummary(Guid Id, string Name, string Category, DateTimeOffset? NextAvailableAt, string? BusinessName, Guid? ProviderId = null);
