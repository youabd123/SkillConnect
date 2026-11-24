namespace SkillConnect.Application.Services;

public sealed class ListPublishedServices(IServiceCatalogReader catalogReader)
{
    public async Task<IReadOnlyList<ServiceSummary>> ExecuteAsync(
        string? category,
        CancellationToken cancellationToken)
    {
        var categoryFilter = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        var listings = await catalogReader.ListPublishedAsync(categoryFilter, cancellationToken);

        return listings
            .Select(listing => new ServiceSummary(listing.Id, listing.Name, listing.Category))
            .ToArray();
    }
}

public sealed record ServiceSummary(Guid Id, string Name, string Category);
