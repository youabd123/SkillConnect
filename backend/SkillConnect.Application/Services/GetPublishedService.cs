using SkillConnect.Domain.Services;

namespace SkillConnect.Application.Services;

public sealed class GetPublishedService(IServiceCatalogReader catalogReader)
{
    public Task<ServiceDetails?> ExecuteAsync(Guid id, CancellationToken cancellationToken) =>
        catalogReader.FindPublishedAsync(id, cancellationToken);
}

public sealed record ServiceDetails(Guid Id, string Name, string Description, string Category, string? BusinessName,
    Guid? ProviderId = null, ServiceDeliveryMode? DeliveryMode = null, string? City = null);
