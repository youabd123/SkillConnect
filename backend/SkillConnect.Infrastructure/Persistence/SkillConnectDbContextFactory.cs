using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SkillConnect.Infrastructure.Persistence;

public sealed class SkillConnectDbContextFactory : IDesignTimeDbContextFactory<SkillConnectDbContext>
{
    public SkillConnectDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__SkillConnect");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Set ConnectionStrings__SkillConnect before running EF Core tools.");
        }

        var options = new DbContextOptionsBuilder<SkillConnectDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new SkillConnectDbContext(options);
    }
}
