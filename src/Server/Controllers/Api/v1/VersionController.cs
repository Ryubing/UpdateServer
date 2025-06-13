using Microsoft.AspNetCore.Mvc;
using Ryujinx.Systems.Update.Common;
using Ryujinx.Systems.Updater.Common;
using Ryujinx.Systems.Updater.Server.Services.GitLab;

namespace Ryujinx.Systems.Updater.Server.Controllers;

[Route($"{Constants.FullRouteName_Api_Version}")]
[ApiController]
public class VersionController : ControllerBase
{
    [HttpGet($"{Constants.StableRoute}/{Constants.RouteName_Latest}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json")]
    public async Task<ActionResult<object>> GetLatestStable(
        [FromKeyedServices("stableCache")] VersionCache vcache,
        [FromQuery] string? os = null,
        [FromQuery] string? arch = null
        )
    {
        var lck = await vcache.TakeLockAsync();
        
        var latest = vcache.Latest;
        
        lck.Dispose();
        
        if (latest is null)
            return NotFound();
        
        if (!os.TryParseAsSupportedPlatform(out var supportedPlatform))
            return BadRequest($"Unknown platform '{os}'");
        
        if (!arch.TryParseAsSupportedArchitecture(out var supportedArch))
            return BadRequest($"Unknown architecture '{arch}'");
        
        return Ok(new VersionResponse
        {
            Version = latest.Tag,
            ArtifactUrl = latest.GetUrlFor(supportedPlatform, supportedArch),
            ReleaseUrl = vcache.FormatReleaseUrl(latest.Tag)
        });
    }
    
    [HttpGet($"{Constants.CanaryRoute}/{Constants.RouteName_Latest}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json")]
    public async Task<ActionResult<object>> GetLatestCanary(
        [FromKeyedServices("canaryCache")] VersionCache vcache,
        [FromQuery] string? os = null,
        [FromQuery] string? arch = null
    )
    {
        var lck = await vcache.TakeLockAsync();
        
        var latest = vcache.Latest;
        
        lck.Dispose();

        if (latest is null)
            return NotFound();

        if (!os.TryParseAsSupportedPlatform(out var supportedPlatform))
            return BadRequest($"Unknown platform '{os}'");
        
        if (!arch.TryParseAsSupportedArchitecture(out var supportedArch))
            return BadRequest($"Unknown architecture '{arch}'");

        return Ok(new VersionResponse
        {
            Version = latest.Tag,
            ArtifactUrl = latest.GetUrlFor(supportedPlatform, supportedArch),
            ReleaseUrl = vcache.FormatReleaseUrl(latest.Tag)
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
        using (var _ = await vcache.TakeLockAsync())
        {
            if (vcache[version] is { } cacheEntry)
                return Ok(cacheEntry);
        }
        
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
        using (var _ = await vcache.TakeLockAsync())
        {
            if (vcache[version] is { } cacheEntry)
                return Ok(cacheEntry);
        }
        
        return NotFound();
    }
}