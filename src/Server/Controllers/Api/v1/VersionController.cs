using Microsoft.AspNetCore.Mvc;
using Ryujinx.Systems.Update.Common;
using Ryujinx.Systems.Update.Server.Services.GitLab;

namespace Ryujinx.Systems.Update.Server.Controllers;

[Route($"{Constants.FullRouteName_Api_Version}")]
[ApiController]
public class VersionController : ControllerBase
{
    [HttpGet($"{Constants.StableRoute}/{Constants.RouteName_Latest}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json")]
    public async Task<ActionResult<VersionResponse>> GetLatestStable(
        [FromKeyedServices("stableCache")] VersionCache vcache,
        [FromQuery] string? os = null,
        [FromQuery] string? arch = null
        )
    {
        if (!os.TryParseAsSupportedPlatform(out var supportedPlatform))
            return BadRequest($"Unknown platform '{os}'");
        
        if (!arch.TryParseAsSupportedArchitecture(out var supportedArch))
            return BadRequest($"Unknown architecture '{arch}'");
        
        if (await vcache.GetReleaseAsync(c => c.GetLatest(supportedPlatform, supportedArch)) is not {} latest)
            return NotFound();
        
        return Ok(new VersionResponse
        {
            Version = latest.Tag,
            ArtifactUrl = latest.GetUrlFor(supportedPlatform, supportedArch),
            ReleaseUrlFormat = vcache.ReleaseUrlFormat
        });
    }
    
    [HttpGet($"{Constants.CanaryRoute}/{Constants.RouteName_Latest}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json")]
    public async Task<ActionResult<VersionResponse>> GetLatestCanary(
        [FromKeyedServices("canaryCache")] VersionCache vcache,
        [FromQuery] string? os = null,
        [FromQuery] string? arch = null
    )
    {
        if (!os.TryParseAsSupportedPlatform(out var supportedPlatform))
            return BadRequest($"Unknown platform '{os}'");
        
        if (!arch.TryParseAsSupportedArchitecture(out var supportedArch))
            return BadRequest($"Unknown architecture '{arch}'");
        
        if (await vcache.GetReleaseAsync(c => c.GetLatest(supportedPlatform, supportedArch)) is not { } latest)
            return NotFound();

        return Ok(new VersionResponse
        {
            Version = latest.Tag,
            ArtifactUrl = latest.GetUrlFor(supportedPlatform, supportedArch),
            ReleaseUrlFormat = vcache.ReleaseUrlFormat
        });
    }
    
    [HttpGet($"{Constants.StableRoute}/{{version}}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json")]
    public async Task<ActionResult<VersionCacheEntry>> GetSpecificStable(
        [FromKeyedServices("stableCache")] VersionCache vcache,
        string version
    )
    {
        if (await vcache.GetReleaseAsync(c => c[version]) is { } cacheEntry)
            return Ok(cacheEntry);
        
        return NotFound();
    }
    
    [HttpGet($"{Constants.CanaryRoute}/{{version}}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json")]
    public async Task<ActionResult<VersionCacheEntry>> GetSpecificCanary(
        [FromKeyedServices("canaryCache")] VersionCache vcache,
        string version
    )
    {
        if (await vcache.GetReleaseAsync(c => c[version]) is { } cacheEntry)
            return Ok(cacheEntry);
        
        return NotFound();
    }
}