namespace SkillConnect.Application.Services;

public sealed class GetProviderProfile(IServiceCatalogReader catalogReader, TimeProvider clock)
{
    public Task<PublicProviderProfile?> ExecuteAsync(Guid providerId, CancellationToken cancellationToken) =>
        catalogReader.FindProviderAsync(providerId, clock.GetUtcNow(), cancellationToken);
}

public sealed record PublicProviderProfile(
    Guid Id, string BusinessName, string? BusinessDescription, IReadOnlyList<ServiceSummary> Services);
