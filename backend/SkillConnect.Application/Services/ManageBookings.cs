namespace SkillConnect.Application.Services;

public sealed class ManageBookings(IBookingStore store, TimeProvider clock)
{
    public Task<BookingSummary?> BookAsync(
        Guid serviceId, Guid slotId, Guid customerId, CancellationToken cancellationToken) =>
        store.TryBookAsync(serviceId, slotId, customerId, clock.GetUtcNow(), cancellationToken);

    public Task<IReadOnlyList<BookingSummary>> ListForCustomerAsync(
        Guid customerId, CancellationToken cancellationToken) =>
        store.ListForCustomerAsync(customerId, cancellationToken);

    public Task<IReadOnlyList<BookingSummary>> ListForProviderAsync(
        Guid providerId, CancellationToken cancellationToken) =>
        store.ListForProviderAsync(providerId, cancellationToken);
}
