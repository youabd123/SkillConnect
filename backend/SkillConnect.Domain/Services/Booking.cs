namespace SkillConnect.Domain.Services;

public sealed class Booking(Guid id, Guid slotId, Guid customerId)
{
    public Guid Id { get; } = id;
    public Guid SlotId { get; } = slotId;
    public Guid CustomerId { get; } = customerId;
}
