namespace SkillConnect.Domain.Services;

public sealed class ServiceListing
{
    public ServiceListing(Guid providerId, string name, string description, string category)
    {
        if (providerId == Guid.Empty)
        {
            throw new ArgumentException("A provider is required.", nameof(providerId));
        }

        UpdateDetails(name, description, category);
        Id = Guid.NewGuid();
        ProviderId = providerId;
    }

    public Guid Id { get; }
    public Guid ProviderId { get; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Category { get; private set; }
    public bool IsPublished { get; private set; }
    public ServiceDeliveryMode? DeliveryMode { get; private set; }
    public string? City { get; private set; }

    [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(Name), nameof(Description), nameof(Category))]
    public void UpdateDetails(string name, string description, string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        Name = name.Trim();
        Description = description.Trim();
        Category = category.Trim();
    }

    public void Publish()
    {
        IsPublished = true;
    }

    public static bool IsValidDelivery(ServiceDeliveryMode mode, string? city) => mode switch
    {
        ServiceDeliveryMode.OnSite or ServiceDeliveryMode.Both => city?.Trim().Length is > 0 and <= 100,
        ServiceDeliveryMode.Remote => string.IsNullOrWhiteSpace(city),
        _ => false,
    };

    public void SetDelivery(ServiceDeliveryMode mode, string? city)
    {
        if (!IsValidDelivery(mode, city))
        {
            throw new ArgumentException("Choose a delivery mode and provide a city only for on-site services.");
        }

        DeliveryMode = mode;
        City = mode == ServiceDeliveryMode.Remote ? null : city!.Trim();
    }
}

public enum ServiceDeliveryMode
{
    OnSite = 1,
    Remote = 2,
    Both = 3,
}
