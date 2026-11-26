using Microsoft.EntityFrameworkCore;
using SkillConnect.Application.Services;
using SkillConnect.Domain.Services;

namespace SkillConnect.Infrastructure.Persistence;

public sealed class SqlServerAvailabilityStore(SkillConnectDbContext dbContext) : IAvailabilityStore
{
    public Task<bool> IsOwnedAsync(Guid serviceId, Guid providerId, CancellationToken cancellationToken) =>
        dbContext.ServiceListings.AnyAsync(
            service => service.Id == serviceId && service.ProviderId == providerId, cancellationToken);

    public Task<bool> IsPublishedAsync(Guid serviceId, CancellationToken cancellationToken) =>
        dbContext.ServiceListings.AnyAsync(service => service.Id == serviceId && service.IsPublished, cancellationToken);

    public async Task<IReadOnlyList<AvailabilitySlot>> ListUpcomingAsync(
        Guid serviceId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return await dbContext.GetUpcomingAvailableSlots(now).AsNoTracking()
            .Where(slot => slot.ServiceId == serviceId)
            .OrderBy(slot => slot.StartsAt)
            .ThenBy(slot => slot.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AvailabilitySlot slot, CancellationToken cancellationToken)
    {
        dbContext.AvailabilitySlots.Add(slot);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RemoveUpcomingAsync(
        Guid serviceId, Guid slotId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var slot = await dbContext.GetUpcomingAvailableSlots(now).SingleOrDefaultAsync(
            slot => slot.Id == slotId && slot.ServiceId == serviceId, cancellationToken);
        if (slot is null)
        {
            return false;
        }

        slot.Withdraw();
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}
