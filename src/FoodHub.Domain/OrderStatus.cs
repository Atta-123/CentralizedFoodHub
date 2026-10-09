namespace FoodHub.Domain;
public enum OrderStatus
{
    PendingAcceptance,
    Accepted,
    Preparing,
    ReadyForPickup,
    Delivered,
    Rejected,
    Cancelled
}
