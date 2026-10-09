using Microsoft.Data.SqlClient;
using FoodHub.Application;
using FoodHub.Domain;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Infrastructure;
public class NearbyDistance
{
    public Guid Id { get; set; }
    public double DistanceKm { get; set; }
}

public partial class FoodService
{
    public async Task<Page<RestaurantDto>> Nearby(NearbyRequest input, CancellationToken ct)
    {
        var latitude = input.Latitude!.Value;
        var longitude = input.Longitude!.Value;
        // Conservative latitude bounding band uses the coordinate index before exact distance.
        var minLat = Math.Max(-90, latitude - input.RadiusKm / 110);
        var maxLat = Math.Min(90, latitude + input.RadiusKm / 110);
        object[] parameters = [new SqlParameter("@nearbyLat", latitude), new SqlParameter("@nearbyLng", longitude), new SqlParameter("@minLat", minLat), new SqlParameter("@maxLat", maxLat)];
        // Parameterized SQL Server spherical distance, consistent with checkout.
        var distances = db.Database.SqlQueryRaw<NearbyDistance>("""
            SELECT Id, 6371.0 * 2 * ASIN(SQRT(CASE WHEN h > 1 THEN 1 WHEN h < 0 THEN 0 ELSE h END)) AS DistanceKm
            FROM (
                SELECT Id, POWER(SIN((Latitude - @nearbyLat) * PI() / 360), 2)
                     + COS(@nearbyLat * PI() / 180) * COS(Latitude * PI() / 180)
                     * POWER(SIN((Longitude - @nearbyLng) * PI() / 360), 2) AS h
                FROM Restaurants
                WHERE Latitude >= @minLat AND Latitude <= @maxLat AND Longitude IS NOT NULL
            ) AS candidates
            """, parameters);
        var query =
            from r in db.Restaurants.AsNoTracking()join d in distances on r.Id equals d.Id
            where r.IsOpen && d.DistanceKm <= input.RadiusKm && d.DistanceKm <= r.DeliveryRadiusKm
            select new
            {
                Restaurant = r,
                d.DistanceKm
            };
        if (current.Role == Roles.Restaurant)
            query = query.Where(x => x.Restaurant.OwnerId == current.Id);
        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var term = input.Search.Trim();
            query = query.Where(x => x.Restaurant.Name.Contains(term) || x.Restaurant.Cuisine.Contains(term));
        }

        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(x => x.DistanceKm).ThenBy(x => x.Restaurant.Id).Skip((input.Page - 1) * input.PageSize).Take(input.PageSize).Select(x => new RestaurantDto(x.Restaurant.Id, x.Restaurant.Name, x.Restaurant.Cuisine, x.Restaurant.Address, x.Restaurant.CommissionPercent, x.Restaurant.IsOpen, null, x.Restaurant.Latitude, x.Restaurant.Longitude, x.Restaurant.DeliveryRadiusKm, x.DistanceKm)).ToListAsync(ct);
        return new(total, input.Page, input.PageSize, rows);
    }
}
