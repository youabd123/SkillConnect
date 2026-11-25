using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SkillConnect.Application.Services;
using SkillConnect.Domain.Services;

namespace SkillConnect.Api.Tests;

public sealed class ServiceCatalogEndpointTests
{
    [Theory]
    [InlineData("/api/services", new[] { "Cleaning", "Gardening" })]
    [InlineData("/api/services?category=%20Gardening%20", new[] { "Gardening" })]
    [InlineData("/api/services?category=Painting", new string[0])]
    public async Task GetServicesReturnsCatalogResults(string path, string[] expectedCategories)
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IServiceCatalogReader>();
                services.AddSingleton<IServiceCatalogReader, TestCatalogReader>();
            }));
        using var client = factory.CreateClient();

        var listings = await client.GetFromJsonAsync<ServiceSummary[]>(path);

        Assert.NotNull(listings);
        Assert.Equal(expectedCategories, listings.Select(listing => listing.Category));
    }

    private sealed class TestCatalogReader : IServiceCatalogReader
    {
        private readonly ServiceListing[] listings =
        [
            CreateListing("Cleaning", published: true),
            CreateListing("Gardening", published: true),
            CreateListing("Gardening", published: false),
        ];

        public Task<IReadOnlyList<ServiceListing>> ListPublishedAsync(
            string? category,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ServiceListing> results = listings
                .Where(listing => listing.IsPublished && (category is null || listing.Category == category))
                .ToArray();

            return Task.FromResult(results);
        }

        private static ServiceListing CreateListing(string category, bool published)
        {
            var listing = new ServiceListing(Guid.NewGuid(), $"{category} service", "Description", category);
            if (published)
            {
                listing.Publish();
            }

            return listing;
        }
    }
}
