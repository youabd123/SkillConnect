using SkillConnect.Domain.Services;

namespace SkillConnect.Application.Services;

public sealed class ListPublishedServices(IServiceCatalogReader catalogReader, TimeProvider clock)
{
    public Task<IReadOnlyList<ServiceSummary>> ExecuteAsync(
        string? category,
        string? search,
        string? city,
        bool remote,
        CancellationToken cancellationToken)
    {
        var filter = new ServiceCatalogFilter(Normalize(category), Normalize(search), Normalize(city), remote);
        return catalogReader.ListPublishedAsync(filter, clock.GetUtcNow(), cancellationToken);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record ServiceCatalogFilter(string? Category, string? Search, string? City, bool Remote);

public sealed record ServiceSummary(Guid Id, string Name, string Category, DateTimeOffset? NextAvailableAt,
    string? BusinessName, Guid? ProviderId = null, ServiceDeliveryMode? DeliveryMode = null, string? City = null);
