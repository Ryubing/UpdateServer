using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using RyujinxUpdate.Model;
using RyujinxUpdate.Services.GitLab;

namespace RyujinxUpdate.Controllers;

// api/version/stable/latest

[Route("api/[controller]")]
[ApiController]
public class VersionController : ControllerBase
{
    [HttpGet("stable/latest")]
    [HttpGet("latest")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json")]
    [OpenApiOperation("Get latest stable", "Gets the latest Stable release URLs.")]
    public async Task<ActionResult<VersionDumpResponse>> GetLatest(
        [FromKeyedServices("stableCache")] VersionCache vcache
        )
    {
        var latest = vcache.Latest;

        if (latest is null)
            return NotFound();
        
        return Ok(VersionDumpResponse.FromVersionCache(latest));
    }
}