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
}
