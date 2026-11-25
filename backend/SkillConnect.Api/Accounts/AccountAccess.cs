using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using SkillConnect.Infrastructure.Identity;

namespace SkillConnect.Api.Accounts;

internal static class AccountAccess
{
    public const string CustomerPolicy = "Customer";
    public const string ProviderPolicy = "Provider";

    public static void RequireAccountRole(this AuthorizationPolicyBuilder policy, AccountRole role)
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(async context =>
            context.Resource is HttpContext httpContext &&
            await httpContext.RequestServices.GetRequiredService<UserManager<SkillConnectUser>>()
                .GetUserAsync(context.User) is { } account && account.AccountRole == role);
    }

    public static Guid GetAccountId(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
