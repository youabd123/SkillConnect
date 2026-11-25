using Microsoft.Data.SqlClient;
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
        return await dbContext.AvailabilitySlots.AsNoTracking()
            .Where(slot => slot.ServiceId == serviceId && slot.StartsAt > now &&
                !dbContext.Bookings.Any(booking => booking.SlotId == slot.Id))
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
        var slot = await dbContext.AvailabilitySlots.SingleOrDefaultAsync(
            slot => slot.Id == slotId && slot.ServiceId == serviceId && slot.StartsAt > now &&
                !dbContext.Bookings.Any(booking => booking.SlotId == slot.Id), cancellationToken);
        if (slot is null)
        {
            return false;
        }

        dbContext.AvailabilitySlots.Remove(slot);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 547 })
        {
            return false;
        }
    }
}
