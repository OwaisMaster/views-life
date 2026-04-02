using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]

public sealed class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]

    public ActionResult<object> Get()
    {
        return Ok(
        new
        {
            status = "ok",
            service = "ViewsLife.Api",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            utcTime = DateTime.UtcNow
        }
        );
    }
}