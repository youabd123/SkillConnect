using System.Security.Claims;
using SkillConnect.Application.Services;
using SkillConnect.Infrastructure.Persistence;
using static SkillConnect.Api.Accounts.AccountAccess;

namespace SkillConnect.Api.Services;

public static class AvailabilityEndpoints
{
    public static void AddServiceAvailability(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IAvailabilityStore, SqlServerAvailabilityStore>();
        services.AddScoped<ManageAvailability>();
    }

    public static void MapProviderAvailability(this RouteGroupBuilder services)
    {
        var availability = services.MapGroup("/{serviceId:guid}/availability");
        availability.MapGet("", ListForProviderAsync);
        availability.MapPost("", CreateAsync);
        availability.MapDelete("/{slotId:guid}", RemoveAsync);
    }

    public static void MapPublicAvailability(this WebApplication app) =>
        app.MapGet("/api/services/{serviceId:guid}/availability", ListPublicAsync);

    private static async Task<IResult> ListForProviderAsync(
        Guid serviceId, ClaimsPrincipal user, ManageAvailability availability, CancellationToken cancellationToken)
    {
        var times = await availability.ListForProviderAsync(serviceId, GetAccountId(user), cancellationToken);
        return times is null ? Results.NotFound() : Results.Ok(times);
    }

    private static async Task<IResult> ListPublicAsync(
        Guid serviceId, ManageAvailability availability, CancellationToken cancellationToken)
    {
        var times = await availability.ListPublicAsync(serviceId, cancellationToken);
        return times is null ? Results.NotFound() : Results.Ok(times);
    }

    private static async Task<IResult> CreateAsync(
        Guid serviceId, AvailabilityInput input, ClaimsPrincipal user, ManageAvailability availability,
        CancellationToken cancellationToken)
    {
        if (input.StartsAt is not { } startsAt || input.EndsAt is not { } endsAt)
        {
            return InvalidTime();
        }

        var result = await availability.CreateAsync(serviceId, GetAccountId(user), startsAt, endsAt, cancellationToken);
        return result.Status switch
        {
            AvailabilityCreationStatus.Created => Results.Created((string?)null, result.Time),
            AvailabilityCreationStatus.InvalidTime => InvalidTime(),
            _ => Results.NotFound(),
        };
    }

    private static async Task<IResult> RemoveAsync(
        Guid serviceId, Guid slotId, ClaimsPrincipal user, ManageAvailability availability, CancellationToken cancellationToken)
    {
        return await availability.RemoveAsync(serviceId, GetAccountId(user), slotId, cancellationToken)
            ? Results.NoContent()
            : Results.NotFound();
    }

    private static IResult InvalidTime() =>
        Results.BadRequest(new { message = "Start must be in the future and end must be after start." });

    private sealed record AvailabilityInput(DateTimeOffset? StartsAt, DateTimeOffset? EndsAt);
}
