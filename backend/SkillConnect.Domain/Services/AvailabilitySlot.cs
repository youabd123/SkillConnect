namespace SkillConnect.Domain.Services;

public sealed class AvailabilitySlot
{
    private AvailabilitySlot(Guid id, Guid serviceId, DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        Id = id;
        ServiceId = serviceId;
        StartsAt = startsAt;
        EndsAt = endsAt;
    }

    public Guid Id { get; }
    public Guid ServiceId { get; }
    public DateTimeOffset StartsAt { get; }
    public DateTimeOffset EndsAt { get; }
    public bool IsWithdrawn { get; private set; }
    public Guid Revision { get; private set; }

    public void RecordBooking() => Revision = Guid.NewGuid();

    public void Withdraw()
    {
        IsWithdrawn = true;
        Revision = Guid.NewGuid();
    }

    public static AvailabilitySlot? Create(
        Guid serviceId, DateTimeOffset startsAt, DateTimeOffset endsAt, DateTimeOffset now)
    {
        if (serviceId == Guid.Empty || startsAt <= now || endsAt <= startsAt)
        {
            return null;
        }

        return new AvailabilitySlot(Guid.NewGuid(), serviceId, startsAt.ToUniversalTime(), endsAt.ToUniversalTime());
    }
}
