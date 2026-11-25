using System.Security.Claims;
using SkillConnect.Application.Services;
using SkillConnect.Infrastructure.Persistence;
using static SkillConnect.Api.Accounts.AccountAccess;

namespace SkillConnect.Api.Services;

public static class BookingEndpoints
{
    public static void AddServiceBookings(this IServiceCollection services)
    {
        services.AddScoped<IBookingStore, SqlServerBookingStore>();
        services.AddScoped<ManageBookings>();
    }

    public static void MapServiceBookings(this WebApplication app)
    {
        app.MapPost("/api/services/{serviceId:guid}/availability/{slotId:guid}/booking", BookAsync)
            .RequireAuthorization(CustomerPolicy);
        app.MapGet("/api/accounts/bookings", ListForCustomerAsync).RequireAuthorization(CustomerPolicy);
        app.MapGet("/api/provider/bookings", ListForProviderAsync).RequireAuthorization(ProviderPolicy);
    }

    private static async Task<IResult> BookAsync(Guid serviceId, Guid slotId, HttpRequest request,
        ClaimsPrincipal user, ManageBookings bookings, CancellationToken cancellationToken)
    {
        if (!request.HasJsonContentType())
        {
            return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
        }

        var booking = await bookings.BookAsync(serviceId, slotId, GetAccountId(user), cancellationToken);
        return booking is null
            ? Results.Conflict(new { message = "This time is no longer available." })
            : Results.Created((string?)null, booking);
    }

    private static async Task<IResult> ListForCustomerAsync(
        ClaimsPrincipal user, ManageBookings bookings, CancellationToken cancellationToken) =>
        Results.Ok(await bookings.ListForCustomerAsync(GetAccountId(user), cancellationToken));

    private static async Task<IResult> ListForProviderAsync(
        ClaimsPrincipal user, ManageBookings bookings, CancellationToken cancellationToken) =>
        Results.Ok(await bookings.ListForProviderAsync(GetAccountId(user), cancellationToken));
}
