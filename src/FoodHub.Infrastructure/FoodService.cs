using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FoodHub.Application;
using FoodHub.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Infrastructure;
public partial class FoodService(FoodDbContext db, ICurrentUser current, ITokenService tokens, IPasswordHasher<User> hasher) : IFoodService
{
    static string Email(string value) => value.Trim().ToLowerInvariant();
    static long Cents(decimal amount) => checked((long)decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));
    static RestaurantDto RestaurantView(Restaurant x, bool owner) => new(x.Id, x.Name, x.Cuisine, x.Address, x.CommissionPercent, x.IsOpen, owner ? x.Owner.Email : null, x.Latitude, x.Longitude, x.DeliveryRadiusKm);
    static MenuDto MenuView(MenuItem x) => new(x.Id, x.RestaurantId, x.Name, x.Description, x.PriceCents / 100m, x.IsAvailable);
    static OrderDto OrderView(Order x) => new(x.Id, x.RestaurantId, x.RestaurantName, x.CustomerName, x.Address, x.Phone, x.SubtotalCents / 100m, x.CommissionPercent, x.CommissionCents / 100m, x.RestaurantPayableCents / 100m, x.Status.ToString(), x.CreatedAt, x.Version, x.Items.Select(i => new OrderLineDto(i.Name, i.UnitPriceCents / 100m, i.Quantity)).ToList(), x.History.OrderBy(h => h.ChangedAt).Select(h => new HistoryDto(h.Status.ToString(), h.ChangedAt)).ToList(), x.DeliveryLatitude, x.DeliveryLongitude);
    static (int, int) Paging(int page, int size) => (Math.Clamp(page, 1, 1000000), Math.Clamp(size, 1, 50));
    void Require(string role)
    {
        if (current.Role != role)
            throw new AppException(403, "This action is not permitted.");
    }

    IQueryable<Order> ScopeOrders() => current.Role switch
    {
        Roles.Admin => db.Orders,
        Roles.Restaurant => db.Orders.Where(x => x.Restaurant.OwnerId == current.Id),
        Roles.Customer => db.Orders.Where(x => x.CustomerId == current.Id),
        _ => db.Orders.Where(x => false)};
    async Task<Restaurant> EditableRestaurant(Guid id, CancellationToken ct)
    {
        var r = await db.Restaurants.Include(x => x.Owner).SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new AppException(404, "Restaurant not found.");
        if (current.Role != Roles.Admin && !(current.Role == Roles.Restaurant && r.OwnerId == current.Id))
            throw new AppException(403, "You cannot manage this restaurant.");
        return r;
    }

    public async Task<AuthResult> Login(LoginRequest input, CancellationToken ct)
    {
        var email = Email(input.Email);
        var u = await db.Users.SingleOrDefaultAsync(x => x.Email == email, ct);
        if (u is null || hasher.VerifyHashedPassword(u, u.PasswordHash, input.Password) == PasswordVerificationResult.Failed)
            throw new AppException(401, "Invalid email or password.");
        return new(tokens.Create(u), u.Id, u.Name, u.Role);
    }

    public async Task<AuthResult> Register(RegisterRequest input, CancellationToken ct)
    {
        var email = Email(input.Email);
        if (await db.Users.AnyAsync(x => x.Email == email, ct))
            throw new AppException(409, "Email is already registered.");
        var u = new User
        {
            Name = input.Name.Trim(),
            Email = email,
            Role = Roles.Customer
        };
        u.PasswordHash = hasher.HashPassword(u, input.Password);
        db.Users.Add(u);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new AppException(409, "Email is already registered.");
        }

        return new(tokens.Create(u), u.Id, u.Name, u.Role);
    }

    public async Task<Page<RestaurantDto>> Restaurants(string? search, int page, int size, CancellationToken ct)
    {
        (page, size) = Paging(page, size);
        var query = db.Restaurants.AsNoTracking();
        if (current.Role == Roles.Restaurant)
            query = query.Where(x => x.OwnerId == current.Id);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Name.Contains(term) || x.Cuisine.Contains(term));
        }

        var count = await query.CountAsync(ct);
        var rows = await query.Include(x => x.Owner).OrderBy(x => x.Name).ThenBy(x => x.Id).Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new(count, page, size, rows.Select(x => RestaurantView(x, current.Role == Roles.Admin)).ToList());
    }

    public async Task<RestaurantDto> CreateRestaurant(RestaurantRequest input, CancellationToken ct)
    {
        Require(Roles.Admin);
        var email = Email(input.OwnerEmail);
        if (await db.Users.AnyAsync(x => x.Email == email, ct))
            throw new AppException(409, "Use a new owner email for this restaurant.");
        var u = new User
        {
            Name = (input.Name.Trim() + " Owner")[..Math.Min(80, input.Name.Trim().Length + 6)],
            Email = email,
            Role = Roles.Restaurant
        };
        u.PasswordHash = hasher.HashPassword(u, input.OwnerPassword);
        var r = new Restaurant
        {
            Name = input.Name.Trim(),
            Cuisine = input.Cuisine.Trim(),
            Address = input.Address.Trim(),
            CommissionPercent = decimal.Round(input.CommissionPercent, 2),
            Latitude = input.Latitude,
            Longitude = input.Longitude,
            DeliveryRadiusKm = input.DeliveryRadiusKm,
            Owner = u
        };
        db.Restaurants.Add(r);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new AppException(409, "Restaurant owner email is already in use.");
        }

        return RestaurantView(r, true);
    }

    public async Task<RestaurantDto> UpdateRestaurant(Guid id, RestaurantSettings input, CancellationToken ct)
    {
        Require(Roles.Admin);
        var r = await EditableRestaurant(id, ct);
        r.CommissionPercent = decimal.Round(input.CommissionPercent, 2);
        r.IsOpen = input.IsOpen;
        if (input.Latitude.HasValue != input.Longitude.HasValue)
            throw new AppException(400, "Provide both latitude and longitude.");
        if (input.Latitude.HasValue)
        {
            r.Latitude = input.Latitude;
            r.Longitude = input.Longitude;
        }

        if (input.DeliveryRadiusKm.HasValue)
            r.DeliveryRadiusKm = input.DeliveryRadiusKm.Value;
        await db.SaveChangesAsync(ct);
        return RestaurantView(r, true);
    }

    public async Task<List<MenuDto>> Menu(Guid restaurantId, CancellationToken ct)
    {
        var r = await db.Restaurants.AsNoTracking().SingleOrDefaultAsync(x => x.Id == restaurantId, ct) ?? throw new AppException(404, "Restaurant not found.");
        if (current.Role == Roles.Restaurant && r.OwnerId != current.Id)
            throw new AppException(403, "Restaurant access denied.");
        var query = db.MenuItems.AsNoTracking().Where(x => x.RestaurantId == restaurantId);
        if (current.Role != Roles.Admin && current.Role != Roles.Restaurant)
            query = query.Where(x => x.IsAvailable);
        return (await query.OrderBy(x => x.Name).Take(200).ToListAsync(ct)).Select(MenuView).ToList();
    }

    public async Task<MenuDto> SaveMenu(Guid restaurantId, Guid? itemId, MenuRequest input, CancellationToken ct)
    {
        await EditableRestaurant(restaurantId, ct);
        var item = itemId is null ? new MenuItem
        {
            RestaurantId = restaurantId
        }

        : await db.MenuItems.SingleOrDefaultAsync(x => x.Id == itemId && x.RestaurantId == restaurantId, ct) ?? throw new AppException(404, "Menu item not found.");
        if (itemId is null && await db.MenuItems.CountAsync(x => x.RestaurantId == restaurantId, ct) >= 200)
            throw new AppException(400, "This starter supports at most 200 menu items per restaurant.");
        item.Name = input.Name.Trim();
        item.Description = input.Description.Trim();
        item.PriceCents = Cents(input.Price);
        item.IsAvailable = input.IsAvailable;
        if (itemId is null)
            db.MenuItems.Add(item);
        await db.SaveChangesAsync(ct);
        return MenuView(item);
    }

    public async Task<OrderDto> PlaceOrder(PlaceOrderRequest input, string key, CancellationToken ct)
    {
        Require(Roles.Customer);
        if (string.IsNullOrWhiteSpace(key) || key.Length > 80)
            throw new AppException(400, "Idempotency-Key header is required (maximum 80 characters).");
        if (input.Items.Any(x => x.Quantity < 1 || x.Quantity > 20))
            throw new AppException(400, "Each item quantity must be between 1 and 20.");
        if (input.Items.Select(x => x.MenuItemId).Distinct().Count() != input.Items.Count)
            throw new AppException(400, "Duplicate menu items are not allowed.");
        var canonical = new
        {
            input.RestaurantId,
            Address = input.Address.Trim(),
            Phone = input.Phone.Trim(),
            input.DeliveryLatitude,
            input.DeliveryLongitude,
            Items = input.Items.OrderBy(x => x.MenuItemId).ToList()
        };
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(canonical))));
        var previous = await db.Orders.AsNoTracking().Include(x => x.Items).Include(x => x.History).AsSplitQuery().SingleOrDefaultAsync(x => x.CustomerId == current.Id && x.IdempotencyKey == key, ct);
        if (previous is not null)
        {
            if (previous.RequestHash != hash)
                throw new AppException(409, "This checkout key was already used for a different order.");
            return OrderView(previous);
        }

        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var r = await db.Restaurants.SingleOrDefaultAsync(x => x.Id == input.RestaurantId, ct) ?? throw new AppException(404, "Restaurant not found.");
        if (!r.IsOpen)
            throw new AppException(400, "Restaurant is closed.");
        if (r.Latitude is null || r.Longitude is null)
            throw new AppException(400, "Restaurant location is not configured. Contact the restaurant.");
        if (input.DeliveryLatitude is null || input.DeliveryLongitude is null)
            throw new AppException(400, "Select a delivery location before checkout.");
        if (Geo.DistanceKm(input.DeliveryLatitude.Value, input.DeliveryLongitude.Value, r.Latitude.Value, r.Longitude.Value) > r.DeliveryRadiusKm)
            throw new AppException(400, "Your delivery location is outside this restaurant delivery range.");
        var ids = input.Items.Select(x => x.MenuItemId).ToArray();
        var items = await db.MenuItems.AsNoTracking().Where(x => x.RestaurantId == r.Id && x.IsAvailable && ids.Contains(x.Id)).ToListAsync(ct);
        if (items.Count != ids.Length)
            throw new AppException(400, "One or more items are unavailable or belong to another restaurant.");
        var customer = await db.Users.AsNoTracking().SingleAsync(x => x.Id == current.Id, ct);
        var order = new Order
        {
            CustomerId = current.Id,
            RestaurantId = r.Id,
            RestaurantName = r.Name,
            CustomerName = customer.Name,
            DeliveryLatitude = input.DeliveryLatitude,
            DeliveryLongitude = input.DeliveryLongitude,
            Address = input.Address.Trim(),
            Phone = input.Phone.Trim(),
            CommissionPercent = r.CommissionPercent,
            IdempotencyKey = key,
            RequestHash = hash
        };
        foreach (var line in input.Items)
        {
            var item = items.Single(x => x.Id == line.MenuItemId);
            order.Items.Add(new OrderItem { MenuItemId = item.Id, Name = item.Name, UnitPriceCents = item.PriceCents, Quantity = line.Quantity });
        }

        order.SubtotalCents = order.Items.Sum(x => checked(x.UnitPriceCents * x.Quantity));
        order.CommissionCents = OrderRules.Commission(order.SubtotalCents, r.CommissionPercent);
        order.RestaurantPayableCents = order.SubtotalCents - order.CommissionCents;
        order.History.Add(new OrderHistory { Status = order.Status, ActorId = current.Id });
        db.Orders.Add(order);
        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            var existing = await db.Orders.AsNoTracking().Include(x => x.Items).Include(x => x.History).AsSplitQuery().SingleOrDefaultAsync(x => x.CustomerId == current.Id && x.IdempotencyKey == key, ct);
            if (existing is not null && existing.RequestHash == hash)
                return OrderView(existing);
            throw new AppException(409, "Checkout conflicted with another request. Retry with the same checkout key.");
        }

        return OrderView(order);
    }

    public async Task<Page<OrderDto>> Orders(int page, int size, CancellationToken ct)
    {
        (page, size) = Paging(page, size);
        var query = ScopeOrders().AsNoTracking();
        var count = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((page - 1) * size).Take(size).Include(x => x.Items).Include(x => x.History).AsSplitQuery().ToListAsync(ct);
        return new(count, page, size, rows.Select(OrderView).ToList());
    }

    public async Task<OrderDto> ChangeStatus(Guid id, StatusRequest input, CancellationToken ct)
    {
        var o = await ScopeOrders().Include(x => x.Items).Include(x => x.History).AsSplitQuery().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new AppException(404, "Order not found.");
        if (!Enum.TryParse<OrderStatus>(input.Status, false, out var next) || !Enum.IsDefined(next))
            throw new AppException(400, "Invalid order status.");
        // Restaurant prepares food; only admin confirms delivery in this MVP (rider module is not implemented).
        if (current.Role == Roles.Customer && next != OrderStatus.Cancelled)
            throw new AppException(403, "Customers can only cancel pending orders.");
        if (current.Role == Roles.Restaurant && next is OrderStatus.Delivered or OrderStatus.Cancelled)
            throw new AppException(403, "This status requires platform administration.");
        if (o.Version != input.Version)
            throw new AppException(409, "Order changed. Refresh before trying again.");
        if (!OrderRules.CanTransition(o.Status, next))
            throw new AppException(409, "This order status transition is not allowed.");
        o.Status = next;
        o.Version = Guid.NewGuid();
        var history = new OrderHistory
        {
            OrderId = o.Id,
            Status = next,
            ActorId = current.Id
        };
        db.Set<OrderHistory>().Add(history);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException(409, "Order changed. Refresh before trying again.");
        }

        return OrderView(o);
    }

    public async Task<SummaryDto> Summary(CancellationToken ct)
    {
        Require(Roles.Admin);
        var all = await db.Orders.CountAsync(ct);
        var delivered = db.Orders.Where(x => x.Status == OrderStatus.Delivered);
        var totals = await delivered.GroupBy(x => 1).Select(g => new { Count = g.Count(), Sales = g.Sum(x => x.SubtotalCents), Commission = g.Sum(x => x.CommissionCents), Payable = g.Sum(x => x.RestaurantPayableCents) }).SingleOrDefaultAsync(ct);
        return new(await db.Restaurants.CountAsync(ct), all, totals?.Count ?? 0, (totals?.Sales ?? 0) / 100m, (totals?.Commission ?? 0) / 100m, (totals?.Payable ?? 0) / 100m);
    }

    public async Task<List<SettlementDto>> Settlements(CancellationToken ct)
    {
        if (current.Role != Roles.Admin && current.Role != Roles.Restaurant)
            throw new AppException(403, "Access denied.");
        var rows = await ScopeOrders().AsNoTracking().Where(x => x.Status == OrderStatus.Delivered).GroupBy(x => new { x.RestaurantId, x.RestaurantName }).Select(g => new { g.Key.RestaurantId, g.Key.RestaurantName, Count = g.Count(), Sales = g.Sum(x => x.SubtotalCents), Commission = g.Sum(x => x.CommissionCents), Payable = g.Sum(x => x.RestaurantPayableCents) }).OrderBy(x => x.RestaurantName).ToListAsync(ct);
        return rows.Select(x => new SettlementDto(x.RestaurantId, x.RestaurantName, x.Count, x.Sales / 100m, x.Commission / 100m, x.Payable / 100m)).ToList();
    }
}
