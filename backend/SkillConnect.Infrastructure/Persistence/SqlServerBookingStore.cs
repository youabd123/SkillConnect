using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SkillConnect.Application.Services;
using SkillConnect.Domain.Services;

namespace SkillConnect.Infrastructure.Persistence;

public sealed class SqlServerBookingStore(SkillConnectDbContext dbContext) : IBookingStore
{
    public async Task<BookingSummary?> TryBookAsync(
        Guid serviceId, Guid slotId, Guid customerId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var availableTime = await (
            from slot in dbContext.AvailabilitySlots
            join service in dbContext.ServiceListings on slot.ServiceId equals service.Id
            where slot.Id == slotId && service.Id == serviceId && service.IsPublished && slot.StartsAt > now &&
                !slot.IsWithdrawn && !dbContext.Bookings.Any(booking => booking.SlotId == slot.Id && !booking.IsCancelled)
            select new { service.Name, Slot = slot })
            .SingleOrDefaultAsync(cancellationToken);

        if (availableTime is null)
        {
            return null;
        }

        var booking = new Booking(Guid.NewGuid(), slotId, customerId);
        dbContext.Bookings.Add(booking);
        // Compete with withdrawal on the slot's concurrency token in the same save.
        availableTime.Slot.RecordBooking();
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return null;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 or 547 })
        {
            return null;
        }

        return new BookingSummary(booking.Id, serviceId, availableTime.Name,
            availableTime.Slot.StartsAt, availableTime.Slot.EndsAt, booking.IsCancelled);
    }

    public async Task<BookingCancellation> CancelAsync(
        Guid bookingId, Guid customerId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var result = await (
            from booking in dbContext.Bookings
            join slot in dbContext.AvailabilitySlots on booking.SlotId equals slot.Id
            where booking.Id == bookingId && booking.CustomerId == customerId
            select new { Booking = booking, slot.StartsAt })
            .SingleOrDefaultAsync(cancellationToken);
        if (result is null)
        {
            return BookingCancellation.NotFound;
        }
        if (result.Booking.IsCancelled)
        {
            return BookingCancellation.Cancelled;
        }
        if (result.StartsAt <= now)
        {
            return BookingCancellation.Started;
        }

        result.Booking.Cancel();
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A concurrent cancellation already changed the status.
        }

        return BookingCancellation.Cancelled;
    }

    public Task<IReadOnlyList<BookingSummary>> ListForCustomerAsync(Guid customerId, CancellationToken cancellationToken) =>
        ListAsync(customerId, forCustomer: true, cancellationToken);

    public Task<IReadOnlyList<BookingSummary>> ListForProviderAsync(Guid providerId, CancellationToken cancellationToken) =>
        ListAsync(providerId, forCustomer: false, cancellationToken);

    private async Task<IReadOnlyList<BookingSummary>> ListAsync(
        Guid accountId, bool forCustomer, CancellationToken cancellationToken)
    {
        return await (
            from booking in dbContext.Bookings
            join slot in dbContext.AvailabilitySlots on booking.SlotId equals slot.Id
            join service in dbContext.ServiceListings on slot.ServiceId equals service.Id
            where (forCustomer ? booking.CustomerId : service.ProviderId) == accountId
            orderby slot.StartsAt, booking.Id
            select new BookingSummary(booking.Id, service.Id, service.Name, slot.StartsAt, slot.EndsAt, booking.IsCancelled))
            .ToListAsync(cancellationToken);
    }
}
