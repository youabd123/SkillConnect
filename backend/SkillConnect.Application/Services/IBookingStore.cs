namespace SkillConnect.Application.Services;

public interface IBookingStore
{
    Task<BookingSummary?> TryBookAsync(
        Guid serviceId, Guid slotId, Guid customerId, DateTimeOffset now, CancellationToken cancellationToken);

    Task<IReadOnlyList<BookingSummary>> ListForCustomerAsync(Guid customerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<BookingSummary>> ListForProviderAsync(Guid providerId, CancellationToken cancellationToken);
}

public sealed record BookingSummary(
    Guid Id, Guid ServiceId, string ServiceName, DateTimeOffset StartsAt, DateTimeOffset EndsAt);
