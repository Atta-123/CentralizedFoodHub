using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FoodHub.Infrastructure;
// EF tooling can generate migrations/scripts without starting the API or connecting to SQL Server.
public sealed class FoodDbContextFactory : IDesignTimeDbContextFactory<FoodDbContext>
{
    public FoodDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__SqlServer") ?? "Server=(localdb)\\MSSQLLocalDB;Database=FoodHubSqlServer;Trusted_Connection=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<FoodDbContext>().UseSqlServer(connectionString).Options;
        return new FoodDbContext(options);
    }
}
