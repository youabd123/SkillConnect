using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using SkillConnect.Application.Services;
using SkillConnect.Infrastructure.Identity;
using SkillConnect.Infrastructure.Persistence;

namespace SkillConnect.Api.Services;

public static class ProviderServiceEndpoints
{
    private const string ProviderPolicy = "Provider";

    public static void AddProviderServices(this IServiceCollection services)
    {
        services.AddScoped<IProviderServiceStore, SqlServerProviderServiceStore>();
        services.AddScoped<ManageProviderServices>();
        services.AddAuthorizationBuilder().AddPolicy(ProviderPolicy, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(async context =>
                context.Resource is HttpContext httpContext &&
                await httpContext.RequestServices.GetRequiredService<UserManager<SkillConnectUser>>()
                    .GetUserAsync(context.User) is { AccountRole: AccountRole.Provider });
        });
    }

    public static void MapProviderServices(this WebApplication app)
    {
        var services = app.MapGroup("/api/provider/services").RequireAuthorization(ProviderPolicy);
        services.MapGet("", ListAsync);
        services.MapPost("", CreateAsync);
        services.MapPut("/{id:guid}", UpdateAsync);
        services.MapPut("/{id:guid}/publication", PublishAsync);
    }

    private static async Task<IResult> ListAsync(
        ClaimsPrincipal user, ManageProviderServices services, CancellationToken cancellationToken) =>
        Results.Ok(await services.ListAsync(GetProviderId(user), cancellationToken));

    private static async Task<IResult> CreateAsync(
        ServiceInput input, ClaimsPrincipal user, ManageProviderServices services, CancellationToken cancellationToken)
    {
        if (!input.IsValid())
        {
            return InvalidService();
        }

        var service = await services.CreateAsync(GetProviderId(user),
            input.Name!, input.Description!, input.Category!, cancellationToken);
        return Results.Created((string?)null, service);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id, ServiceInput input, ClaimsPrincipal user, ManageProviderServices services, CancellationToken cancellationToken)
    {
        if (!input.IsValid())
        {
            return InvalidService();
        }

        var service = await services.UpdateAsync(GetProviderId(user), id,
            input.Name!, input.Description!, input.Category!, cancellationToken);
        return service is null ? Results.NotFound() : Results.Ok(service);
    }

    private static async Task<IResult> PublishAsync(
        Guid id, ClaimsPrincipal user, ManageProviderServices services, CancellationToken cancellationToken)
    {
        var service = await services.PublishAsync(GetProviderId(user), id, cancellationToken);
        return service is null ? Results.NotFound() : Results.Ok(service);
    }

    private static Guid GetProviderId(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static IResult InvalidService() => Results.BadRequest(new
    {
        message = "Enter a name (up to 120 characters), description (up to 2,000), and category (up to 80).",
    });

    private sealed record ServiceInput(string? Name, string? Description, string? Category)
    {
        public bool IsValid() =>
            Name?.Trim().Length is > 0 and <= 120 &&
            Description?.Trim().Length is > 0 and <= 2000 &&
            Category?.Trim().Length is > 0 and <= 80;
    }
}
