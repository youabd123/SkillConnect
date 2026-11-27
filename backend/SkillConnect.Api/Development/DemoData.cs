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
    private static readonly DemoCompany[] Companies =
    [
        new(ProviderEmail, "SkillConnect Demo", "Vi hjälper till med enklare trädgårdsarbete.", "Trädgård",
        [
            new("Trädgårdsskötsel", "Hjälp med gräsklippning och lättare trädgårdsarbete."),
        ]),
        new("it@skillconnect.test", "Norrsken IT", "Vi hjälper privatpersoner med datorer och trådlösa nätverk.", "IT & teknik",
        [
            new("Datorhjälp", "Få hjälp med en långsam dator, uppdateringar eller installation av program."),
            new("Wi-Fi och hemnätverk", "Felsök anslutningen och få hjälp att ställa in router och trådlöst nätverk."),
        ]),
        new("frisor@skillconnect.test", "Salong Linje", "En fiktiv frisörsalong för klippning, styling och skäggvård.", "Frisör & skönhet",
        [
            new("Klippning och styling", "Konsultation, klippning och enkel styling anpassad till din frisyr."),
            new("Skäggtrimning", "Trimning och formning av skägget med råd om skötsel hemma."),
        ]),
        new("restaurang@skillconnect.test", "Bistro Rönn", "En fiktiv kvartersbistro med rätter som följer säsongen.", "Restaurang",
        [
            new("Bord för två", "Reservera ett bord för två personer på en fiktiv kvartersbistro."),
            new("Provsmakning av säsongens meny", "Boka en provsmakning med tre små rätter från ett fiktivt restaurangkök."),
        ]),
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
        await EnsureAccountAsync(users, CustomerEmail, AccountRole.Customer);
        var originalProvider = await EnsureAccountAsync(users, ProviderEmail, AccountRole.Provider, Companies[0]);

        var now = TimeProvider.System.GetUtcNow();
        var tomorrow = new DateTimeOffset(now.UtcDateTime.Date.AddDays(1).AddHours(9), TimeSpan.Zero);
        foreach (var company in Companies)
        {
            var provider = company.Email == ProviderEmail
                ? originalProvider
                : await EnsureAccountAsync(users, company.Email, AccountRole.Provider, company);

            for (var index = 0; index < company.Listings.Length; index++)
            {
                await EnsureListingAsync(db, originalProvider.Id, provider.Id, company.Category,
                    company.Listings[index], now, tomorrow.AddHours(index * 4));
            }
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        app.Logger.LogInformation("Demo database ready with {CompanyCount} providers and {Customer}.", Companies.Length, CustomerEmail);
    }

    private static async Task EnsureListingAsync(
        SkillConnectDbContext db, Guid originalProviderId, Guid providerId, string category,
        DemoListing demo, DateTimeOffset now, DateTimeOffset firstStart)
    {
        var listing = await db.ServiceListings.FirstOrDefaultAsync(service =>
            service.ProviderId == providerId && service.Name == demo.Name);
        if (listing is null && providerId != originalProviderId)
        {
            var originalId = await db.ServiceListings
                .Where(service => service.ProviderId == originalProviderId && service.Name == demo.Name && service.Category == category)
                .Select(service => (Guid?)service.Id)
                .SingleOrDefaultAsync();
            if (originalId is { } id)
            {
                // Keep the existing listing ID, slots, and booking history.
                await db.ServiceListings.Where(service => service.Id == id)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(service => service.ProviderId, providerId));
                listing = await db.ServiceListings.SingleAsync(service => service.Id == id);
            }
        }

        if (listing is null)
        {
            listing = new ServiceListing(providerId, demo.Name, demo.Description, category);
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

    private sealed record DemoCompany(
        string Email, string Name, string Description, string Category, DemoListing[] Listings);

    private sealed record DemoListing(string Name, string Description);

    private static async Task<SkillConnectUser> EnsureAccountAsync(
        UserManager<SkillConnectUser> users, string email, AccountRole role, DemoCompany? company = null)
    {
        var existing = await users.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (existing.AccountRole != role || !existing.EmailConfirmed || !await users.CheckPasswordAsync(existing, Password))
            {
                throw new InvalidOperationException($"Existing account {email} does not match the demo account.");
            }
            existing.BusinessName ??= company?.Name;
            return existing;
        }

        var user = new SkillConnectUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            EmailConfirmed = true,
            AccountRole = role,
            BusinessName = company?.Name,
            BusinessDescription = company?.Description,
        };
        var result = await users.CreateAsync(user, Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Cannot create demo account {email}: {string.Join(", ", result.Errors.Select(error => error.Code))}");
        }
        return user;
    }
}
