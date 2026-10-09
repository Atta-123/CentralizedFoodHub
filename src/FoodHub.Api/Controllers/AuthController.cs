using FoodHub.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodHub.Api.Controllers;
[ApiController, Route("api/auth"), EnableRateLimiting("auth")]
public class AuthController(IFoodService service) : ControllerBase
{
    [HttpPost("login")]
    public Task<AuthResult> Login(LoginRequest input, CancellationToken ct) => service.Login(input, ct);
    [HttpPost("register")]
    public Task<AuthResult> Register(RegisterRequest input, CancellationToken ct) => service.Register(input, ct);
}
