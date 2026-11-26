using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SkillConnect.Application.Services;
using SkillConnect.Infrastructure.Identity;
using SkillConnect.Infrastructure.Persistence;

namespace SkillConnect.Api.Tests;

public sealed class ProviderServiceEndpointTests
{
    private const string ServicesPath = "/api/provider/services";
    private const string Password = "TestPassword123!";

    [Fact]
    public async Task OnlyProvidersCanCreateDrafts()
    {
        using var factory = new TestFactory();
        using var anonymous = factory.CreateAccountClient();
        using var customer = factory.CreateAccountClient();
        using var provider = factory.CreateAccountClient();
        await SignInAsync(factory, customer, "customer@example.test", AccountRole.Customer);
        var providerId = await SignInAsync(factory, provider, "provider@example.test", AccountRole.Provider);
        var input = new
        {
            name = " Garden care ",
            description = "General garden maintenance",
            category = "Gardening",
            providerId = Guid.NewGuid(),
        };

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync(ServicesPath, input)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.PostAsJsonAsync(ServicesPath, input)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await provider.PostAsJsonAsync(ServicesPath, new { name = " " })).StatusCode);

        using var response = await provider.PostAsJsonAsync(ServicesPath, input);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var draft = await response.Content.ReadFromJsonAsync<ProviderService>();
        Assert.NotNull(draft);
        Assert.Equal("Garden care", draft.Name);
        Assert.False(draft.IsPublished);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/services/{draft.Id}")).StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SkillConnectDbContext>();
        var listing = await database.ServiceListings.SingleAsync();
        Assert.Equal(providerId, listing.ProviderId);
    }

    [Fact]
    public async Task OnlyTheOwnerCanListEditAndPublishTheirService()
    {
        using var factory = new TestFactory();
        using var visitor = factory.CreateAccountClient();
        using var customer = factory.CreateAccountClient();
        using var owner = factory.CreateAccountClient();
        using var otherProvider = factory.CreateAccountClient();
        await SignInAsync(factory, customer, "customer@example.test", AccountRole.Customer);
        await SignInAsync(factory, owner, "owner@example.test", AccountRole.Provider);
        await SignInAsync(factory, otherProvider, "other@example.test", AccountRole.Provider);

        using var response = await owner.PostAsJsonAsync(ServicesPath,
            new { name = "Draft name", description = "Draft description", category = "Gardening" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var draft = await response.Content.ReadFromJsonAsync<ProviderService>();
        Assert.NotNull(draft);
        var editPath = $"{ServicesPath}/{draft.Id}";
        var publishPath = $"{editPath}/publication";
        var edit = new { name = "Garden care", description = "Updated description", category = "Gardening" };

        Assert.Equal(draft, Assert.Single((await owner.GetFromJsonAsync<ProviderService[]>(ServicesPath))!));
        Assert.Empty((await otherProvider.GetFromJsonAsync<ProviderService[]>(ServicesPath))!);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync(ServicesPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.PutAsJsonAsync(editPath, edit)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.PutAsync(publishPath, null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherProvider.PutAsJsonAsync(editPath, edit)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherProvider.PutAsync(publishPath, null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync(editPath, edit)).StatusCode);
        Assert.Empty((await visitor.GetFromJsonAsync<ServiceSummary[]>("/api/services"))!);

        using var published = await owner.PutAsync(publishPath, null);
        Assert.Equal(HttpStatusCode.OK, published.StatusCode);
        Assert.True((await published.Content.ReadFromJsonAsync<ProviderService>())!.IsPublished);
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsync(publishPath, null)).StatusCode);

        var publicListing = Assert.Single((await visitor.GetFromJsonAsync<ServiceSummary[]>("/api/services"))!);
        Assert.Equal(draft.Id, publicListing.Id);
        Assert.Equal(edit.name, publicListing.Name);
        var details = await visitor.GetFromJsonAsync<ServiceDetails>($"/api/services/{draft.Id}");
        Assert.Equal(edit.description, details?.Description);
    }

    [Fact]
    public async Task OnlyOwnersManageTimesAndVisitorsSeeUpcomingTimesOnPublishedServices()
    {
        using var factory = new TestFactory();
        using var visitor = factory.CreateAccountClient();
        using var customer = factory.CreateAccountClient();
        using var owner = factory.CreateAccountClient();
        using var otherProvider = factory.CreateAccountClient();
        await SignInAsync(factory, customer, "customer@example.test", AccountRole.Customer);
        await SignInAsync(factory, owner, "owner@example.test", AccountRole.Provider);
        await SignInAsync(factory, otherProvider, "other@example.test", AccountRole.Provider);

        using var created = await owner.PostAsJsonAsync(ServicesPath,
            new { name = "Garden care", description = "Seasonal care", category = "Gardening" });
        var listing = await created.Content.ReadFromJsonAsync<ProviderService>();
        Assert.NotNull(listing);
        var managePath = $"{ServicesPath}/{listing.Id}/availability";
        var publicPath = $"/api/services/{listing.Id}/availability";
        var now = factory.Clock.UtcNow;
        var startsAt = now.AddDays(1).ToOffset(TimeSpan.FromHours(2));
        var input = new { startsAt, endsAt = startsAt.AddHours(1) };

        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.PostAsJsonAsync(managePath, input)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.PostAsJsonAsync(managePath, input)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherProvider.GetAsync(managePath)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherProvider.PostAsJsonAsync(managePath, input)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync(managePath,
            new { startsAt = now.AddSeconds(-1), endsAt = now.AddHours(1) })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync(managePath,
            new { startsAt, endsAt = startsAt })).StatusCode);

        using var slotResponse = await owner.PostAsJsonAsync(managePath, input);
        Assert.Equal(HttpStatusCode.Created, slotResponse.StatusCode);
        var slot = await slotResponse.Content.ReadFromJsonAsync<AvailableTime>();
        Assert.NotNull(slot);
        Assert.Equal(startsAt.ToUniversalTime(), slot.StartsAt);
        Assert.Equal(TimeSpan.Zero, slot.StartsAt.Offset);
        Assert.Equal(slot, Assert.Single((await owner.GetFromJsonAsync<AvailableTime[]>(managePath))!));
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync(publicPath)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsync($"{ServicesPath}/{listing.Id}/publication", null)).StatusCode);
        Assert.Equal(slot, Assert.Single((await visitor.GetFromJsonAsync<AvailableTime[]>(publicPath))!));

        using var laterResponse = await owner.PostAsJsonAsync(managePath,
            new { startsAt = now.AddDays(2), endsAt = now.AddDays(2).AddHours(1) });
        var later = await laterResponse.Content.ReadFromJsonAsync<AvailableTime>();
        Assert.NotNull(later);
        Assert.Equal(slot.StartsAt, Assert.Single((await visitor.GetFromJsonAsync<ServiceSummary[]>("/api/services"))!).NextAvailableAt);
        Assert.Equal(HttpStatusCode.NotFound, (await otherProvider.DeleteAsync($"{managePath}/{later.Id}")).StatusCode);

        factory.Clock.UtcNow = now.AddDays(1);
        Assert.Equal(later, Assert.Single((await visitor.GetFromJsonAsync<AvailableTime[]>(publicPath))!));
        Assert.Equal(later.StartsAt, Assert.Single((await visitor.GetFromJsonAsync<ServiceSummary[]>("/api/services"))!).NextAvailableAt);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.DeleteAsync($"{managePath}/{slot.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"{managePath}/{later.Id}")).StatusCode);
        Assert.Empty((await visitor.GetFromJsonAsync<AvailableTime[]>(publicPath))!);
        Assert.Null(Assert.Single((await visitor.GetFromJsonAsync<ServiceSummary[]>("/api/services"))!).NextAvailableAt);
    }

    [Fact]
    public async Task BookingsAreVisibleOnlyToTheCustomerAndServiceOwner()
    {
        using var factory = new TestFactory();
        using var visitor = factory.CreateAccountClient();
        using var owner = factory.CreateAccountClient();
        using var otherProvider = factory.CreateAccountClient();
        using var customer = factory.CreateAccountClient();
        using var otherCustomer = factory.CreateAccountClient();
        await SignInAsync(factory, owner, "owner@example.test", AccountRole.Provider);
        await SignInAsync(factory, otherProvider, "other-provider@example.test", AccountRole.Provider);
        await SignInAsync(factory, customer, "customer@example.test", AccountRole.Customer);
        await SignInAsync(factory, otherCustomer, "other-customer@example.test", AccountRole.Customer);

        using var created = await owner.PostAsJsonAsync(ServicesPath,
            new { name = "Garden care", description = "Seasonal care", category = "Gardening" });
        var listing = await created.Content.ReadFromJsonAsync<ProviderService>();
        Assert.NotNull(listing);
        var availabilityPath = $"{ServicesPath}/{listing.Id}/availability";
        var startsAt = factory.Clock.UtcNow.AddDays(1);
        using var timeCreated = await owner.PostAsJsonAsync(availabilityPath,
            new { startsAt, endsAt = startsAt.AddHours(1) });
        var time = await timeCreated.Content.ReadFromJsonAsync<AvailableTime>();
        Assert.NotNull(time);
        var publicAvailabilityPath = $"/api/services/{listing.Id}/availability";
        var bookingPath = $"{publicAvailabilityPath}/{time.Id}/booking";

        Assert.Equal(HttpStatusCode.Conflict, (await customer.PostAsJsonAsync(bookingPath, new { })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsync($"{ServicesPath}/{listing.Id}/publication", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.PostAsJsonAsync(bookingPath, new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PostAsJsonAsync(bookingPath, new { })).StatusCode);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, (await customer.PostAsync(bookingPath, null)).StatusCode);

        using var booked = await customer.PostAsJsonAsync(bookingPath, new { });
        Assert.Equal(HttpStatusCode.Created, booked.StatusCode);
        var booking = await booked.Content.ReadFromJsonAsync<BookingSummary>();
        Assert.NotNull(booking);
        Assert.Equal(listing.Id, booking.ServiceId);
        Assert.Equal(time.StartsAt, booking.StartsAt);
        Assert.Equal(HttpStatusCode.Conflict, (await otherCustomer.PostAsJsonAsync(bookingPath, new { })).StatusCode);
        Assert.Empty((await visitor.GetFromJsonAsync<AvailableTime[]>(publicAvailabilityPath))!);
        Assert.Null(Assert.Single((await visitor.GetFromJsonAsync<ServiceSummary[]>("/api/services"))!).NextAvailableAt);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.DeleteAsync($"{availabilityPath}/{time.Id}")).StatusCode);
        Assert.Equal(booking, Assert.Single((await customer.GetFromJsonAsync<BookingSummary[]>("/api/accounts/bookings"))!));
        Assert.Equal(booking, Assert.Single((await owner.GetFromJsonAsync<BookingSummary[]>("/api/provider/bookings"))!));
        Assert.Empty((await otherCustomer.GetFromJsonAsync<BookingSummary[]>("/api/accounts/bookings"))!);
        Assert.Empty((await otherProvider.GetFromJsonAsync<BookingSummary[]>("/api/provider/bookings"))!);

        var cancellationPath = $"/api/accounts/bookings/{booking.Id}/cancellation";
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.PutAsync(cancellationPath, null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PutAsync(cancellationPath, null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherCustomer.PutAsync(cancellationPath, null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await customer.PutAsync(cancellationPath, null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await customer.PutAsync(cancellationPath, null)).StatusCode);
        Assert.Equal(time, Assert.Single((await visitor.GetFromJsonAsync<AvailableTime[]>(publicAvailabilityPath))!));
        Assert.Equal(time.StartsAt, Assert.Single((await visitor.GetFromJsonAsync<ServiceSummary[]>("/api/services"))!).NextAvailableAt);

        using var rebooked = await otherCustomer.PostAsJsonAsync(bookingPath, new { });
        Assert.Equal(HttpStatusCode.Created, rebooked.StatusCode);
        var replacement = await rebooked.Content.ReadFromJsonAsync<BookingSummary>();
        Assert.NotNull(replacement);
        Assert.NotEqual(booking.Id, replacement.Id);
        Assert.False(replacement.IsCancelled);
        Assert.True(Assert.Single((await customer.GetFromJsonAsync<BookingSummary[]>("/api/accounts/bookings"))!).IsCancelled);
        var ownerBookings = (await owner.GetFromJsonAsync<BookingSummary[]>("/api/provider/bookings"))!;
        Assert.Equal(2, ownerBookings.Length);
        Assert.Single(ownerBookings, entry => entry.IsCancelled);
        Assert.Single(ownerBookings, entry => !entry.IsCancelled);

        var replacementCancellation = $"/api/accounts/bookings/{replacement.Id}/cancellation";
        Assert.Equal(HttpStatusCode.NoContent, (await otherCustomer.PutAsync(replacementCancellation, null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"{availabilityPath}/{time.Id}")).StatusCode);
        Assert.Empty((await visitor.GetFromJsonAsync<AvailableTime[]>(publicAvailabilityPath))!);
        Assert.Equal(HttpStatusCode.Conflict, (await customer.PostAsJsonAsync(bookingPath, new { })).StatusCode);
        Assert.Equal(2, (await owner.GetFromJsonAsync<BookingSummary[]>("/api/provider/bookings"))!.Count(entry => entry.IsCancelled));

        using var anotherTimeResponse = await owner.PostAsJsonAsync(availabilityPath,
            new { startsAt, endsAt = startsAt.AddHours(1) });
        var anotherTime = await anotherTimeResponse.Content.ReadFromJsonAsync<AvailableTime>();
        Assert.NotNull(anotherTime);
        using var anotherBookingResponse = await customer.PostAsJsonAsync(
            $"{publicAvailabilityPath}/{anotherTime.Id}/booking", new { });
        var anotherBooking = await anotherBookingResponse.Content.ReadFromJsonAsync<BookingSummary>();
        Assert.NotNull(anotherBooking);
        factory.Clock.UtcNow = startsAt;
        Assert.Equal(HttpStatusCode.Conflict, (await customer.PutAsync(
            $"/api/accounts/bookings/{anotherBooking.Id}/cancellation", null)).StatusCode);
    }

    [Fact]
    public async Task ProvidersCanSetTheirPublicBusinessName()
    {
        using var factory = new TestFactory();
        using var visitor = factory.CreateAccountClient();
        using var customer = factory.CreateAccountClient();
        using var provider = factory.CreateAccountClient();
        await SignInAsync(factory, customer, "customer@example.test", AccountRole.Customer);
        await SignInAsync(factory, provider, "private-provider@example.test", AccountRole.Provider);

        using var response = await provider.PostAsJsonAsync(ServicesPath,
            new { name = "Garden care", description = "Seasonal care", category = "Gardening" });
        var service = await response.Content.ReadFromJsonAsync<ProviderService>();
        Assert.NotNull(service);
        Assert.Equal(HttpStatusCode.OK, (await provider.PutAsync($"{ServicesPath}/{service.Id}/publication", null)).StatusCode);
        Assert.Null(Assert.Single((await visitor.GetFromJsonAsync<ServiceSummary[]>("/api/services"))!).BusinessName);

        const string profilePath = "/api/provider/profile";
        var profile = new { businessName = "  Gröna Gården  " };
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.PutAsJsonAsync(profilePath, profile)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.PutAsJsonAsync(profilePath, profile)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await provider.PutAsJsonAsync(profilePath, new { businessName = " " })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await provider.PutAsJsonAsync(profilePath, profile)).StatusCode);
        Assert.Equal("Gröna Gården", (await provider.GetFromJsonAsync<BusinessProfile>("/api/accounts/me"))?.BusinessName);
        Assert.Equal("Gröna Gården", Assert.Single((await visitor.GetFromJsonAsync<ServiceSummary[]>("/api/services"))!).BusinessName);

        var rawDetails = await visitor.GetStringAsync($"/api/services/{service.Id}");
        Assert.Equal("Gröna Gården", System.Text.Json.JsonSerializer.Deserialize<ServiceDetails>(
            rawDetails, System.Text.Json.JsonSerializerOptions.Web)?.BusinessName);
        Assert.DoesNotContain("private-provider@example.test", rawDetails);
    }

    private sealed record BusinessProfile(string? BusinessName);

    private static async Task<Guid> SignInAsync(TestFactory factory, HttpClient client, string email, AccountRole role)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<SkillConnectUser>>();
        var user = new SkillConnectUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            EmailConfirmed = true,
            AccountRole = role,
        };
        Assert.True((await users.CreateAsync(user, Password)).Succeeded);
        using var response = await client.PostAsJsonAsync("/api/accounts/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        return user.Id;
    }

    private sealed class TestFactory : WebApplicationFactory<Program>
    {
        private readonly string databaseName = Guid.NewGuid().ToString();
        public TestClock Clock { get; } = new();

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<SkillConnectDbContext>>();
                services.AddDbContext<SkillConnectDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Clock);
            });
        }

        public HttpClient CreateAccountClient() => CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
        });
    }

    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
