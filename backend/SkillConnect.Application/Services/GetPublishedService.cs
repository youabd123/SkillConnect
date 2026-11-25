namespace SkillConnect.Application.Services;

public sealed class GetPublishedService(IServiceCatalogReader catalogReader)
{
    public async Task<ServiceDetails?> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var listing = await catalogReader.FindPublishedAsync(id, cancellationToken);

        return listing is null
            ? null
            : new ServiceDetails(listing.Id, listing.Name, listing.Description, listing.Category);
    }
}

public sealed record ServiceDetails(Guid Id, string Name, string Description, string Category);
