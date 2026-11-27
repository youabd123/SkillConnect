using SkillConnect.Domain.Services;

namespace SkillConnect.Application.Services;

public sealed class ManageProviderServices(IProviderServiceStore store)
{
    public async Task<ProviderService> CreateAsync(
        Guid providerId, string name, string description, string category, ServiceDeliveryMode? deliveryMode, string? city,
        CancellationToken cancellationToken)
    {
        var listing = new ServiceListing(providerId, name, description, category);
        if (deliveryMode is { } mode)
        {
            listing.SetDelivery(mode, city);
        }
        await store.AddAsync(listing, cancellationToken);
        return ToProviderService(listing);
    }

    public async Task<IReadOnlyList<ProviderService>> ListAsync(Guid providerId, CancellationToken cancellationToken)
    {
        var listings = await store.ListOwnedAsync(providerId, cancellationToken);
        return listings.Select(ToProviderService).ToArray();
    }

    public async Task<ProviderService?> UpdateAsync(
        Guid providerId, Guid id, string name, string description, string category, ServiceDeliveryMode? deliveryMode,
        string? city, CancellationToken cancellationToken)
    {
        var listing = await store.FindOwnedAsync(providerId, id, cancellationToken);
        if (listing is null)
        {
            return null;
        }

        listing.UpdateDetails(name, description, category);
        if (deliveryMode is { } mode)
        {
            listing.SetDelivery(mode, city);
        }
        await store.SaveChangesAsync(cancellationToken);
        return ToProviderService(listing);
    }

    public async Task<ProviderService?> PublishAsync(Guid providerId, Guid id, CancellationToken cancellationToken)
    {
        var listing = await store.FindOwnedAsync(providerId, id, cancellationToken);
        if (listing is null)
        {
            return null;
        }

        listing.Publish();
        await store.SaveChangesAsync(cancellationToken);
        return ToProviderService(listing);
    }

    private static ProviderService ToProviderService(ServiceListing listing) =>
        new(listing.Id, listing.Name, listing.Description, listing.Category, listing.IsPublished, listing.DeliveryMode, listing.City);
}

public sealed record ProviderService(Guid Id, string Name, string Description, string Category, bool IsPublished,
    ServiceDeliveryMode? DeliveryMode, string? City);
