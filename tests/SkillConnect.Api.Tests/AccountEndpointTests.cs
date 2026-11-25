using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SkillConnect.Api.Accounts;
using SkillConnect.Infrastructure.Persistence;

namespace SkillConnect.Api.Tests;

public sealed class AccountEndpointTests
{
    [Theory]
    [InlineData("customer")]
    [InlineData("provider")]
    public async Task EmailConfirmationIsRequiredBeforeSignIn(string role)
    {
        var sender = new TestConfirmationSender();
        var databaseName = Guid.NewGuid().ToString();
        using var rootFactory = new WebApplicationFactory<Program>();
        using var factory = rootFactory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<SkillConnectDbContext>>();
            services.AddDbContext<SkillConnectDbContext>(options => options.UseInMemoryDatabase(databaseName));
            services.RemoveAll<IConfirmationEmailSender>();
            services.AddSingleton<IConfirmationEmailSender>(sender);
        }));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
        });

        const string email = "account@example.test";
        const string password = "TestPassword123!";
        var credentials = new { email, password };

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/accounts/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/api/accounts/register", new { email, password, role })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/accounts/login", credentials)).StatusCode);

        var emailMessage = Assert.Single(sender.Messages);
        Assert.Equal(email, emailMessage.Email);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/accounts/confirm-email",
            new { emailMessage.UserId, Code = "invalid" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/accounts/confirm-email", emailMessage)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/accounts/login", credentials)).StatusCode);
        Assert.Equal(new AccountView(email, role), await client.GetFromJsonAsync<AccountView>("/api/accounts/me"));
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/accounts/logout", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/accounts/me")).StatusCode);
    }

    private sealed record AccountView(string Email, string Role);
    private sealed record ConfirmationMessage(string Email, Guid UserId, string Code);

    private sealed class TestConfirmationSender : IConfirmationEmailSender
    {
        public List<ConfirmationMessage> Messages { get; } = [];

        public Task SendAsync(string email, Guid userId, string code)
        {
            Messages.Add(new ConfirmationMessage(email, userId, code));
            return Task.CompletedTask;
        }
    }
}
