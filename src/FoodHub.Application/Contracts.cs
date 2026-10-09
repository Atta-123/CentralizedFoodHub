using System.ComponentModel.DataAnnotations;
using FoodHub.Domain;

namespace FoodHub.Application;
public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);
public record RegisterRequest([Required, StringLength(80, MinimumLength = 2)] string Name, [Required, EmailAddress, StringLength(200)] string Email, [Required, StringLength(100, MinimumLength = 8)] string Password);
public record AuthResult(string Token, Guid UserId, string Name, string Role);
public record RestaurantRequest([Required, StringLength(100, MinimumLength = 2)] string Name, [Required, StringLength(80)] string Cuisine, [Required, StringLength(300)] string Address, [Range(0, 100)] decimal CommissionPercent, [Required, EmailAddress, StringLength(200)] string OwnerEmail, [Required, StringLength(100, MinimumLength = 8)] string OwnerPassword, [Required, Range(-90, 90)] double? Latitude, [Required, Range(-180, 180)] double? Longitude, [Range(0.1, 50)] double DeliveryRadiusKm = 5);
public record RestaurantSettings([Range(0, 100)] decimal CommissionPercent, bool IsOpen, [Range(-90, 90)] double? Latitude = null, [Range(-180, 180)] double? Longitude = null, [Range(0.1, 50)] double? DeliveryRadiusKm = null);
public record RestaurantDto(Guid Id, string Name, string Cuisine, string Address, decimal CommissionPercent, bool IsOpen, string? OwnerEmail, double? Latitude, double? Longitude, double DeliveryRadiusKm, double? DistanceKm = null);
public record MenuRequest([Required, StringLength(100)] string Name, [StringLength(500)] string Description, [Range(typeof(decimal), "0.01", "100000")] decimal Price, bool IsAvailable);
public record MenuDto(Guid Id, Guid RestaurantId, string Name, string Description, decimal Price, bool IsAvailable);
public record OrderLineRequest(Guid MenuItemId, [Range(1, 20)] int Quantity);
public record PlaceOrderRequest(Guid RestaurantId, [Required, StringLength(300, MinimumLength = 5)] string Address, [Required, StringLength(30, MinimumLength = 5)] string Phone, [Required, MinLength(1), MaxLength(30)] List<OrderLineRequest> Items, [Required, Range(-90, 90)] double? DeliveryLatitude, [Required, Range(-180, 180)] double? DeliveryLongitude);
public record StatusRequest([Required] string Status, Guid Version);
public record OrderLineDto(string Name, decimal UnitPrice, int Quantity);
public record HistoryDto(string Status, DateTime ChangedAt);
public record OrderDto(Guid Id, Guid RestaurantId, string RestaurantName, string CustomerName, string Address, string Phone, decimal Subtotal, decimal CommissionPercent, decimal Commission, decimal RestaurantPayable, string Status, DateTime CreatedAt, Guid Version, List<OrderLineDto> Items, List<HistoryDto> History, double? DeliveryLatitude, double? DeliveryLongitude);
public record Page<T>(int TotalCount, int PageNumber, int PageSize, List<T> Items);
public record SummaryDto(int Restaurants, int Orders, int DeliveredOrders, decimal DeliveredSales, decimal CommissionRevenue, decimal RestaurantPayable);
public record SettlementDto(Guid RestaurantId, string RestaurantName, int DeliveredOrders, decimal Sales, decimal Commission, decimal Payable);
public interface ICurrentUser
{
    Guid Id { get; }

    string Role { get; }
}

public interface ITokenService
{
    string Create(User user);
}

public interface IFoodService
{
    Task<AuthResult> Login(LoginRequest input, CancellationToken ct);
    Task<AuthResult> Register(RegisterRequest input, CancellationToken ct);
    Task<Page<RestaurantDto>> Restaurants(string? search, int page, int size, CancellationToken ct);
    Task<Page<RestaurantDto>> Nearby(NearbyRequest input, CancellationToken ct);
    Task<RestaurantDto> CreateRestaurant(RestaurantRequest input, CancellationToken ct);
    Task<RestaurantDto> UpdateRestaurant(Guid id, RestaurantSettings input, CancellationToken ct);
    Task<List<MenuDto>> Menu(Guid restaurantId, CancellationToken ct);
    Task<MenuDto> SaveMenu(Guid restaurantId, Guid? itemId, MenuRequest input, CancellationToken ct);
    Task<OrderDto> PlaceOrder(PlaceOrderRequest input, string key, CancellationToken ct);
    Task<Page<OrderDto>> Orders(int page, int size, CancellationToken ct);
    Task<OrderDto> ChangeStatus(Guid id, StatusRequest input, CancellationToken ct);
    Task<SummaryDto> Summary(CancellationToken ct);
    Task<List<SettlementDto>> Settlements(CancellationToken ct);
}

public class AppException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}

public class NearbyRequest
{
    [Required, Range(-90, 90)]
    public double? Latitude { get; set; }

    [Required, Range(-180, 180)]
    public double? Longitude { get; set; }

    [Range(0.1, 50)]
    public double RadiusKm { get; set; } = 5;

    [StringLength(100)]
    public string? Search { get; set; }

    [Range(1, 1000000)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 12;
}
