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
    private static readonly DemoListing[] Listings =
    [
        new("Trädgårdsskötsel", "Hjälp med gräsklippning och lättare trädgårdsarbete.", "Trädgård"),
        new("Datorhjälp", "Få hjälp med en långsam dator, uppdateringar eller installation av program.", "IT & teknik"),
        new("Wi-Fi och hemnätverk", "Felsök anslutningen och få hjälp att ställa in router och trådlöst nätverk.", "IT & teknik"),
        new("Klippning och styling", "Konsultation, klippning och enkel styling anpassad till din frisyr.", "Frisör & skönhet"),
        new("Skäggtrimning", "Trimning och formning av skägget med råd om skötsel hemma.", "Frisör & skönhet"),
        new("Bord för två", "Reservera ett bord för två personer på en fiktiv kvartersbistro.", "Restaurang"),
        new("Provsmakning av säsongens meny", "Boka en provsmakning med tre små rätter från ett fiktivt restaurangkök.", "Restaurang"),
    ];

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
        provider.BusinessName ??= "SkillConnect Demo";
        await EnsureAccountAsync(users, CustomerEmail, AccountRole.Customer);

        var now = TimeProvider.System.GetUtcNow();
        var tomorrow = new DateTimeOffset(now.UtcDateTime.Date.AddDays(1).AddHours(9), TimeSpan.Zero);
        for (var index = 0; index < Listings.Length; index++)
        {
            var firstStart = tomorrow.AddDays(index / 3).AddHours(index % 3 * 4);
            await EnsureListingAsync(db, provider.Id, Listings[index], now, firstStart);
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        app.Logger.LogInformation("Demo database ready for {Provider} and {Customer}.", ProviderEmail, CustomerEmail);
    }

    private static async Task EnsureListingAsync(
        SkillConnectDbContext db, Guid providerId, DemoListing demo, DateTimeOffset now, DateTimeOffset firstStart)
    {
        var listing = await db.ServiceListings.FirstOrDefaultAsync(service =>
            service.ProviderId == providerId && service.Name == demo.Name);
        if (listing is null)
        {
            listing = new ServiceListing(providerId, demo.Name, demo.Description, demo.Category);
            db.ServiceListings.Add(listing);
        }
        listing.Publish();

        var upcoming = await db.AvailabilitySlots
            .Where(slot => slot.ServiceId == listing.Id && slot.StartsAt > now && !slot.IsWithdrawn)
            .Select(slot => new
            {
                slot.EndsAt,
                IsBooked = db.Bookings.Any(booking => booking.SlotId == slot.Id && !booking.IsCancelled),
            }).ToListAsync();
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
    }

    private sealed record DemoListing(string Name, string Description, string Category);

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
