namespace FoodHub.Domain;
public static class Geo
{
    public static double DistanceKm(double latitude, double longitude, double otherLatitude, double otherLongitude)
    {
        static double Radians(double value) => value * Math.PI / 180;
        var a = Math.Pow(Math.Sin(Radians(otherLatitude - latitude) / 2), 2) + Math.Cos(Radians(latitude)) * Math.Cos(Radians(otherLatitude)) * Math.Pow(Math.Sin(Radians(otherLongitude - longitude) / 2), 2);
        return 6371 * 2 * Math.Asin(Math.Sqrt(Math.Clamp(a, 0, 1)));
    }
}
