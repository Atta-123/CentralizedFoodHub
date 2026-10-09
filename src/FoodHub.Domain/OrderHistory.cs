namespace FoodHub.Domain;
public class OrderHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public OrderStatus Status { get; set; }
    public Guid ActorId { get; set; }
    public User Actor { get; set; } = null!;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
