namespace FoodHub.Domain;
public class Restaurant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Cuisine { get; set; } = "";
    public string Address { get; set; } = "";
    public decimal CommissionPercent { get; set; }
    public bool IsOpen { get; set; } = true;
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double DeliveryRadiusKm { get; set; } = 5;
    public List<MenuItem> Menu { get; set; } = [];
}
