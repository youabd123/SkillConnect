using SkillConnect.Domain.Services;

namespace SkillConnect.Application.Services;

public interface IAvailabilityStore
{
    Task<bool> IsOwnedAsync(Guid serviceId, Guid providerId, CancellationToken cancellationToken);
    Task<bool> IsPublishedAsync(Guid serviceId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AvailabilitySlot>> ListUpcomingAsync(
        Guid serviceId, DateTimeOffset now, CancellationToken cancellationToken);
    Task AddAsync(AvailabilitySlot slot, CancellationToken cancellationToken);
    Task<bool> RemoveUpcomingAsync(Guid serviceId, Guid slotId, DateTimeOffset now, CancellationToken cancellationToken);
}
