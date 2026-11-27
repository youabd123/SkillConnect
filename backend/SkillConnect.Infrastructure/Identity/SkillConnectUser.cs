using Microsoft.AspNetCore.Identity;

namespace SkillConnect.Infrastructure.Identity;

public sealed class SkillConnectUser : IdentityUser<Guid>
{
    public AccountRole AccountRole { get; init; }
    public string? BusinessName { get; set; }
    public string? BusinessDescription { get; set; }
}

public enum AccountRole
{
    Customer = 1,
    Provider = 2,
}
