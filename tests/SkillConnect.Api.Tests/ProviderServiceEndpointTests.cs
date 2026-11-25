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

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<SkillConnectDbContext>>();
                services.AddDbContext<SkillConnectDbContext>(options => options.UseInMemoryDatabase(databaseName));
            });
        }

        public HttpClient CreateAccountClient() => CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
        });
    }
}
