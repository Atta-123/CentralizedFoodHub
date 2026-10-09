namespace FoodHub.Domain;
public class MenuItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public long PriceCents { get; set; }
    public bool IsAvailable { get; set; } = true;
}
