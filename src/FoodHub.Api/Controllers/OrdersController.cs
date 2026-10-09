using FoodHub.Application;
using FoodHub.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Api.Controllers;
[ApiController, Route("api/orders"), Authorize]
public class OrdersController(IFoodService service) : ControllerBase
{
    [HttpGet]
    public Task<Page<OrderDto>> List(int page = 1, int pageSize = 12, CancellationToken ct = default) => service.Orders(page, pageSize, ct);
    [HttpPost, Authorize(Roles = Roles.Customer)]
    public Task<OrderDto> Place(PlaceOrderRequest input, [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken ct) => service.PlaceOrder(input, key ?? "", ct);
    [HttpPatch("{id:guid}/status")]
    public Task<OrderDto> Status(Guid id, StatusRequest input, CancellationToken ct) => service.ChangeStatus(id, input, ct);
}
