using FoodHub.Application;
using FoodHub.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Api.Controllers;
[ApiController, Route("api/admin"), Authorize(Roles = Roles.Admin)]
public class AdminController(IFoodService service) : ControllerBase
{
    [HttpGet("summary")]
    public Task<SummaryDto> Summary(CancellationToken ct) => service.Summary(ct);
}

[ApiController, Route("api/settlements"), Authorize(Roles = Roles.Admin + "," + Roles.Restaurant)]
public class SettlementsController(IFoodService service) : ControllerBase
{
    [HttpGet]
    public Task<List<SettlementDto>> List(CancellationToken ct) => service.Settlements(ct);
}
