using FoodHub.Application;
using FoodHub.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Api.Controllers;
[ApiController]
[Route("api/restaurants")]
[Authorize]
public class RestaurantsController(
    IFoodService service)
    : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public Task<Page<RestaurantDto>> List(string? search, int page = 1, int pageSize = 12, CancellationToken ct = default)
    {
        return service.Restaurants(search, page, pageSize, ct);
    }

    [HttpGet("nearby")]
    [AllowAnonymous]
    public Task<Page<RestaurantDto>> Nearby([FromQuery] NearbyRequest input, CancellationToken ct)
    {
        return service.Nearby(input, ct);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public Task<RestaurantDto> Create(RestaurantRequest input, CancellationToken ct)
    {
        return service.CreateRestaurant(input, ct);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public Task<RestaurantDto> Update(Guid id, RestaurantSettings input, CancellationToken ct)
    {
        return service.UpdateRestaurant(id, input, ct);
    }

    [HttpGet("{id:guid}/menu")]
    [AllowAnonymous]
    public Task<List<MenuDto>> Menu(Guid id, CancellationToken ct)
    {
        return service.Menu(id, ct);
    }

    [HttpPost("{id:guid}/menu")]
    [Authorize(Roles = Roles.Admin + "," + Roles.Restaurant)]
    public Task<MenuDto> Add(Guid id, MenuRequest input, CancellationToken ct)
    {
        return service.SaveMenu(id, null, input, ct);
    }

    [HttpPut("{id:guid}/menu/{itemId:guid}")]
    [Authorize(Roles = Roles.Admin + "," + Roles.Restaurant)]
    public Task<MenuDto> UpdateMenu(Guid id, Guid itemId, MenuRequest input, CancellationToken ct)
    {
        return service.SaveMenu(id, itemId, input, ct);
    }
}
