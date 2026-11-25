using Microsoft.EntityFrameworkCore;
using SkillConnect.Api.Accounts;
using SkillConnect.Api.Services;
using SkillConnect.Application.Services;
using SkillConnect.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<SkillConnectDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SkillConnect")));
builder.Services.AddScoped<IServiceCatalogReader, SqlServerServiceCatalogReader>();
builder.Services.AddScoped<ListPublishedServices>();
builder.Services.AddScoped<GetPublishedService>();
builder.Services.AddSkillConnectAccounts(builder.Environment);
builder.Services.AddProviderServices();
builder.Services.AddServiceAvailability();

var app = builder.Build();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapSkillConnectAccounts();
app.MapProviderServices();
app.MapPublicAvailability();
app.MapGet("/api/services", async (
    string? category,
    ListPublishedServices listServices,
    CancellationToken cancellationToken) =>
    Results.Ok(await listServices.ExecuteAsync(category, cancellationToken)));

app.MapGet("/api/services/{id:guid}", async (
    Guid id,
    GetPublishedService getService,
    CancellationToken cancellationToken) =>
{
    var service = await getService.ExecuteAsync(id, cancellationToken);
    return service is null ? Results.NotFound() : Results.Ok(service);
});

app.Run();

public partial class Program;
