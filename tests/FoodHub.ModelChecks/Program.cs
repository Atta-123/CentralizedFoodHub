using FoodHub.Domain;
using FoodHub.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Data.SqlClient;

using var db = new FoodDbContextFactory().CreateDbContext([]);
void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
Check(db.Database.ProviderName == "Microsoft.EntityFrameworkCore.SqlServer", "Wrong provider");
Check(db.Model.GetEntityTypes().Count() == 6, "Unexpected entities");
var order = db.Model.FindEntityType(typeof(Order))!;
Check(order.FindProperty(nameof(Order.Version))!.IsConcurrencyToken, "Missing concurrency token");
Check(order.GetIndexes().Any(x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual(new[] { "CustomerId", "IdempotencyKey" })), "Missing checkout unique index");
Check(order.FindProperty(nameof(Order.CommissionPercent))!.GetPrecision() == 5, "Wrong money precision");
Check(order.GetForeignKeys().All(x => x.DeleteBehavior == DeleteBehavior.Restrict), "Order parent deletion must be restricted");
var items = db.Model.FindEntityType(typeof(OrderItem))!;
Check(items.GetForeignKeys().Single(x => x.PrincipalEntityType.ClrType == typeof(Order)).DeleteBehavior == DeleteBehavior.Cascade, "Order items not owned by order");
Check(OrderRules.Commission(100000, 20m) == 20000, "Commission rounding regression");
Check(OrderRules.CanTransition(OrderStatus.ReadyForPickup, OrderStatus.Delivered), "Delivery transition regression");
Check(!OrderRules.CanTransition(OrderStatus.PendingAcceptance, OrderStatus.Delivered), "Invalid delivery transition");
Check(db.Database.GetMigrations().Count() == 1, "Initial migration missing");
Check(!db.Database.HasPendingModelChanges(), "Migration snapshot differs from model");
var script = db.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
Check(script.Contains("CREATE TABLE [Orders]"), "Orders SQL missing");
Check(script.Contains("__EFMigrationsHistory"), "Migration history missing");
Check(script.Contains("CK_Restaurants_Location"), "Coordinate constraint missing");
// SQL translation only: never connects to a SQL Server instance.
var distances = db.Database.SqlQueryRaw<NearbyDistance>("""
SELECT Id, 6371.0 * 2 * ASIN(SQRT(CASE WHEN h > 1 THEN 1 WHEN h < 0 THEN 0 ELSE h END)) AS DistanceKm
FROM (
    SELECT Id, POWER(SIN((Latitude - @nearbyLat) * PI() / 360), 2)
         + COS(@nearbyLat * PI() / 180) * COS(Latitude * PI() / 180)
         * POWER(SIN((Longitude - @nearbyLng) * PI() / 360), 2) AS h
    FROM Restaurants
    WHERE Latitude >= @minLat AND Latitude <= @maxLat AND Longitude IS NOT NULL
) AS candidates
""", new SqlParameter("@nearbyLat", 34.015), new SqlParameter("@nearbyLng", 71.525), new SqlParameter("@minLat", 33.9), new SqlParameter("@maxLat", 34.1));
var nearby = from restaurant in db.Restaurants.AsNoTracking()
             join distance in distances on restaurant.Id equals distance.Id
             where restaurant.IsOpen && distance.DistanceKm <= 5 && distance.DistanceKm <= restaurant.DeliveryRadiusKm
             orderby distance.DistanceKm, restaurant.Id
             select new { restaurant.Id, distance.DistanceKm };
var sql = nearby.Skip(1).Take(2).ToQueryString();
Check(sql.Contains("OFFSET") && sql.Contains("FETCH NEXT") && sql.Contains("@nearbyLat"), "Nearby server-side pagination/parameters failed");
Console.WriteLine("PASS: SQL Server model, relationships, indexes, concurrency, rules, migration snapshot, idempotent script and nearby SQL translation. No database connection was used.");
