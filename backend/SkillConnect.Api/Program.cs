using Microsoft.EntityFrameworkCore;
using SkillConnect.Application.Services;
using SkillConnect.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<SkillConnectDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SkillConnect")));
builder.Services.AddScoped<IServiceCatalogReader, SqlServerServiceCatalogReader>();
builder.Services.AddScoped<ListPublishedServices>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/api/services", async (
    string? category,
    ListPublishedServices listServices,
    CancellationToken cancellationToken) =>
    Results.Ok(await listServices.ExecuteAsync(category, cancellationToken)));

app.Run();

public partial class Program;
