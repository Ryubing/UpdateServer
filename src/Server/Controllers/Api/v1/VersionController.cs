using Microsoft.AspNetCore.Mvc;
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

        if (os is "mac" or "osx" or "macos")
            return Ok(new VersionResponse
            {
                Version = latest.Tag,
                ArtifactUrl = latest.Downloads.MacOS,
                ReleaseUrl = vcache.FormatReleaseUrl(latest.Tag)
            });
        
        var platform = os?.ToLower() switch
        {
            "win" or "w" or "windows" => latest.Downloads.Windows,
            "lin" or "l" or "linux" => latest.Downloads.Linux,
            "ai" or "appimage" or "linuxappimage" or "linuxai" => latest.Downloads.LinuxAppImage,
            _ => null
        };

        if (platform is null && os is not null)
            return BadRequest($"Unknown platform '{os}'");

        var url = arch?.ToLower() switch
        {
            "arm64" or "a64" or "arm" => platform!.Arm64,
            "x64" or "x86-64" or "x86_64" or "amd64" => platform!.X64,
            _ => null
        };
        
        if (url is null && arch is not null)
            return BadRequest($"Unknown architecture '{arch}'");

        if (url is not null)
            return Ok(new VersionResponse
            {
                Version = latest.Tag,
                ArtifactUrl = url,
                ReleaseUrl = vcache.FormatReleaseUrl(latest.Tag)
            });
        
        return Ok(latest);
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

        if (os is "mac" or "osx" or "macos")
            return Ok(new VersionResponse
            {
                Version = latest.Tag,
                ArtifactUrl = latest.Downloads.MacOS,
                ReleaseUrl = vcache.FormatReleaseUrl(latest.Tag)
            });
        
        var platform = os?.ToLower() switch
        {
            "win" or "w" or "windows" => latest.Downloads.Windows,
            "lin" or "l" or "linux" => latest.Downloads.Linux,
            "ai" or "appimage" or "linuxappimage" or "linuxai" => latest.Downloads.LinuxAppImage,
            _ => null
        };

        if (platform is null && os is not null)
            return BadRequest($"Unknown platform '{os}'");

        var url = arch?.ToLower() switch
        {
            "arm64" or "a64" or "arm" => platform!.Arm64,
            "x64" or "x86-64" or "x86_64" or "amd64" => platform!.X64,
            _ => null
        };
        
        if (url is null && arch is not null)
            return BadRequest($"Unknown architecture '{arch}'");

        if (url is not null)
            return Ok(new VersionResponse
            {
                Version = latest.Tag,
                ArtifactUrl = url,
                ReleaseUrl = vcache.FormatReleaseUrl(latest.Tag)
            });
        
        return Ok(latest);
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