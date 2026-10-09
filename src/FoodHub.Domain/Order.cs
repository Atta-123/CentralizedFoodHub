namespace FoodHub.Domain;
public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public User Customer { get; set; } = null!;
    public Guid RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;
    public string RestaurantName { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string Address { get; set; } = "";
    public string Phone { get; set; } = "";
    public long SubtotalCents { get; set; }
    public long CommissionCents { get; set; }
    public long RestaurantPayableCents { get; set; }
    public decimal CommissionPercent { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.PendingAcceptance;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string IdempotencyKey { get; set; } = "";
    public double? DeliveryLatitude { get; set; }
    public double? DeliveryLongitude { get; set; }
    public string RequestHash { get; set; } = "";
    public Guid Version { get; set; } = Guid.NewGuid();
    public List<OrderItem> Items { get; set; } = [];
    public List<OrderHistory> History { get; set; } = [];
}
