namespace FoodHub.Domain;
public static class OrderRules
{
    public static bool CanTransition(OrderStatus from, OrderStatus to) => (from, to) switch
    {
        (OrderStatus.PendingAcceptance, OrderStatus.Accepted or OrderStatus.Rejected or OrderStatus.Cancelled) => true,
        (OrderStatus.Accepted, OrderStatus.Preparing) => true,
        (OrderStatus.Preparing, OrderStatus.ReadyForPickup) => true,
        (OrderStatus.ReadyForPickup, OrderStatus.Delivered) => true,
        _ => false
    };
    public static long Commission(long subtotal, decimal rate) => checked((long)decimal.Round(subtotal * rate / 100m, 0, MidpointRounding.AwayFromZero));
}
