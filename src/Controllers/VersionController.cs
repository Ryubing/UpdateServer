using Microsoft.AspNetCore.Mvc;

namespace RyujinxUpdate.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VersionController : ControllerBase
{
    [HttpGet("latest")]
    [HttpGet]
    public async Task<ActionResult<object>> GetLatest()
    {
        return Ok();
    }
}