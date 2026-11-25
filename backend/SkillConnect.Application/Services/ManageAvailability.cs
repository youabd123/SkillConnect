using SkillConnect.Domain.Services;

namespace SkillConnect.Application.Services;

public sealed class ManageAvailability(IAvailabilityStore store, TimeProvider clock)
{
    public async Task<IReadOnlyList<AvailableTime>?> ListForProviderAsync(
        Guid serviceId, Guid providerId, CancellationToken cancellationToken)
    {
        return await store.IsOwnedAsync(serviceId, providerId, cancellationToken)
            ? await ListUpcomingAsync(serviceId, cancellationToken)
            : null;
    }

    public async Task<IReadOnlyList<AvailableTime>?> ListPublicAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        return await store.IsPublishedAsync(serviceId, cancellationToken)
            ? await ListUpcomingAsync(serviceId, cancellationToken)
            : null;
    }

    public async Task<AvailabilityCreation> CreateAsync(
        Guid serviceId, Guid providerId, DateTimeOffset startsAt, DateTimeOffset endsAt, CancellationToken cancellationToken)
    {
        if (!await store.IsOwnedAsync(serviceId, providerId, cancellationToken))
        {
            return new AvailabilityCreation(AvailabilityCreationStatus.NotFound);
        }

        var slot = AvailabilitySlot.Create(serviceId, startsAt, endsAt, clock.GetUtcNow());
        if (slot is null)
        {
            return new AvailabilityCreation(AvailabilityCreationStatus.InvalidTime);
        }

        await store.AddAsync(slot, cancellationToken);
        return new AvailabilityCreation(AvailabilityCreationStatus.Created, ToAvailableTime(slot));
    }

    public async Task<bool> RemoveAsync(Guid serviceId, Guid providerId, Guid slotId, CancellationToken cancellationToken)
    {
        return await store.IsOwnedAsync(serviceId, providerId, cancellationToken) &&
            await store.RemoveUpcomingAsync(serviceId, slotId, clock.GetUtcNow(), cancellationToken);
    }

    private async Task<IReadOnlyList<AvailableTime>> ListUpcomingAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        var slots = await store.ListUpcomingAsync(serviceId, clock.GetUtcNow(), cancellationToken);
        return slots.Select(ToAvailableTime).ToArray();
    }

    private static AvailableTime ToAvailableTime(AvailabilitySlot slot) => new(slot.Id, slot.StartsAt, slot.EndsAt);
}

public sealed record AvailableTime(Guid Id, DateTimeOffset StartsAt, DateTimeOffset EndsAt);

public enum AvailabilityCreationStatus
{
    Created,
    InvalidTime,
    NotFound,
}

public sealed record AvailabilityCreation(AvailabilityCreationStatus Status, AvailableTime? Time = null);
