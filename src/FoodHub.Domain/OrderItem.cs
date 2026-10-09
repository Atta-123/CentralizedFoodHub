namespace FoodHub.Domain;
public class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = "";
    public long UnitPriceCents { get; set; }
    public int Quantity { get; set; }
}
