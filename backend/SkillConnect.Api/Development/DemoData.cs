using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SkillConnect.Domain.Services;
using SkillConnect.Infrastructure.Identity;
using SkillConnect.Infrastructure.Persistence;

namespace SkillConnect.Api.Development;

internal static class DemoData
{
    private const string ProviderEmail = "provider@skillconnect.test";
    private const string CustomerEmail = "customer@skillconnect.test";
    private const string Password = "SkillConnect-Demo123!";
    private const string ServiceName = "Trädgårdsskötsel";

    public static async Task SeedAsync(WebApplication app)
    {
        var connection = new SqlConnectionStringBuilder(app.Configuration.GetConnectionString("SkillConnect"));
        if (!app.Environment.IsDevelopment() ||
            !string.Equals(connection.DataSource, @"(localdb)\MSSQLLocalDB", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(connection.InitialCatalog, "SkillConnectDemo", StringComparison.OrdinalIgnoreCase) ||
            !connection.IntegratedSecurity)
        {
            throw new InvalidOperationException("Demo data requires Development and the local SkillConnectDemo database.");
        }

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SkillConnectDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<SkillConnectUser>>();
        await db.Database.MigrateAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var provider = await EnsureAccountAsync(users, ProviderEmail, AccountRole.Provider);
        await EnsureAccountAsync(users, CustomerEmail, AccountRole.Customer);

        var listing = await db.ServiceListings.FirstOrDefaultAsync(service =>
            service.ProviderId == provider.Id && service.Name == ServiceName);
        if (listing is null)
        {
            listing = new ServiceListing(provider.Id, ServiceName, "Hjälp med gräsklippning och lättare trädgårdsarbete.", "Trädgård");
            db.ServiceListings.Add(listing);
        }
        listing.Publish();

        var now = TimeProvider.System.GetUtcNow();
        var upcoming = await db.AvailabilitySlots.Where(slot => slot.ServiceId == listing.Id && slot.StartsAt > now)
            .Select(slot => new
            {
                slot.EndsAt,
                IsBooked = db.Bookings.Any(booking => booking.SlotId == slot.Id),
            }).ToListAsync();
        var firstStart = new DateTimeOffset(now.UtcDateTime.Date.AddDays(1).AddHours(9), TimeSpan.Zero);
        if (upcoming.Count > 0)
        {
            var nextStart = upcoming.Max(slot => slot.EndsAt).AddHours(1);
            if (nextStart > firstStart)
            {
                firstStart = nextStart;
            }
        }

        for (var index = upcoming.Count(slot => !slot.IsBooked); index < 2; index++)
        {
            db.AvailabilitySlots.Add(AvailabilitySlot.Create(listing.Id, firstStart, firstStart.AddHours(1), now)!);
            firstStart = firstStart.AddHours(2);
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        app.Logger.LogInformation("Demo database ready for {Provider} and {Customer}.", ProviderEmail, CustomerEmail);
    }

    private static async Task<SkillConnectUser> EnsureAccountAsync(
        UserManager<SkillConnectUser> users, string email, AccountRole role)
    {
        var existing = await users.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (existing.AccountRole != role || !existing.EmailConfirmed || !await users.CheckPasswordAsync(existing, Password))
            {
                throw new InvalidOperationException($"Existing account {email} does not match the demo account.");
            }
            return existing;
        }

        var user = new SkillConnectUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            EmailConfirmed = true,
            AccountRole = role,
        };
        var result = await users.CreateAsync(user, Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Cannot create demo account {email}: {string.Join(", ", result.Errors.Select(error => error.Code))}");
        }
        return user;
    }
}
