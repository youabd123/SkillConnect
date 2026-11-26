using System.Net;
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
        using var factory = CreateFactory(new TestCatalogReader());
        using var client = factory.CreateClient();

        var listings = await client.GetFromJsonAsync<ServiceSummary[]>(path);

        Assert.NotNull(listings);
        Assert.Equal(expectedCategories, listings.Select(listing => listing.Category));
    }

    [Fact]
    public async Task GetServiceReturnsPublishedDetails()
    {
        var catalog = new TestCatalogReader();
        using var factory = CreateFactory(catalog);
        using var client = factory.CreateClient();
        var listing = catalog.PublishedListing;

        var details = await client.GetFromJsonAsync<ServiceDetails>($"/api/services/{listing.Id}");

        Assert.Equal(new ServiceDetails(listing.Id, listing.Name, listing.Description, listing.Category), details);
    }

    [Theory]
    [InlineData("draft")]
    [InlineData("unknown")]
    [InlineData("invalid")]
    public async Task GetServiceReturnsNotFoundForUnavailableIds(string unavailableId)
    {
        var catalog = new TestCatalogReader();
        using var factory = CreateFactory(catalog);
        using var client = factory.CreateClient();
        var id = unavailableId switch
        {
            "draft" => catalog.DraftListing.Id.ToString(),
            "unknown" => Guid.NewGuid().ToString(),
            _ => "not-a-guid",
        };

        using var response = await client.GetAsync($"/api/services/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(TestCatalogReader catalog)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IServiceCatalogReader>();
                services.AddSingleton<IServiceCatalogReader>(catalog);
            }));
    }

    private sealed class TestCatalogReader : IServiceCatalogReader
    {
        private readonly ServiceListing[] listings =
        [
            CreateListing("Cleaning", published: true),
            CreateListing("Gardening", published: true),
            CreateListing("Gardening", published: false),
        ];

        public ServiceListing PublishedListing => listings[0];
        public ServiceListing DraftListing => listings[2];

        public Task<ServiceListing?> FindPublishedAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(listings.SingleOrDefault(listing => listing.Id == id && listing.IsPublished));
        }

        public Task<IReadOnlyList<ServiceSummary>> ListPublishedAsync(
            string? category,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ServiceSummary> results = listings
                .Where(listing => listing.IsPublished && (category is null || listing.Category == category))
                .Select(listing => new ServiceSummary(listing.Id, listing.Name, listing.Category, null))
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
