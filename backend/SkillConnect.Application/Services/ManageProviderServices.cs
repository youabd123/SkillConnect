using SkillConnect.Domain.Services;

namespace SkillConnect.Application.Services;

public sealed class ManageProviderServices(IProviderServiceStore store)
{
    public async Task<ProviderService> CreateAsync(
        Guid providerId, string name, string description, string category, CancellationToken cancellationToken)
    {
        var listing = new ServiceListing(providerId, name, description, category);
        await store.AddAsync(listing, cancellationToken);
        return new ProviderService(listing.Id, listing.Name, listing.Description, listing.Category, listing.IsPublished);
    }
}

public sealed record ProviderService(Guid Id, string Name, string Description, string Category, bool IsPublished);
