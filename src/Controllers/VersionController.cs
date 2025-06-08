using Microsoft.AspNetCore.Mvc;
using RyujinxUpdate.Model;
using RyujinxUpdate.Services.GitLab;

namespace RyujinxUpdate.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VersionController : ControllerBase
{
    [HttpGet("stable/latest")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json")]
    public ActionResult<object> GetLatestStable(
        [FromKeyedServices("stableCache")] VersionCache vcache,
        [FromQuery] string? os = null,
        [FromQuery] string? arch = null
        )
    {
        var latest = vcache.Latest;
        
        if (latest is null)
            return NotFound();

        if (os is "mac" or "osx" or "macos")
            return Ok(new VersionResponse
            {
                Version = latest.Tag,
                ArtifactUrl = latest.Downloads.MacOS
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
                ArtifactUrl = url
            });
        
        return Ok(latest);
    }
    
    [HttpGet("canary/latest")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json")]
    public ActionResult<object> GetLatestCanary(
        [FromKeyedServices("canaryCache")] VersionCache vcache,
        [FromQuery] string? os = null,
        [FromQuery] string? arch = null
    )
    {
        var latest = vcache.Latest;

        if (latest is null)
            return NotFound();

        if (os is "mac" or "osx" or "macos")
            return Ok(new VersionResponse
            {
                Version = latest.Tag,
                ArtifactUrl = latest.Downloads.MacOS
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
                ArtifactUrl = url
            });
        
        return Ok(latest);
    }
    
    [HttpGet("stable/{version}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json")]
    public ActionResult<VersionCache.Entry> GetSpecificStable(
        [FromKeyedServices("stableCache")] VersionCache vcache,
        string version
    )
    {
        if (vcache[version] is { } cacheEntry)
            return Ok(cacheEntry);

        return NotFound();
    }
    
    [HttpGet("canary/{version}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json")]
    public ActionResult<VersionCache.Entry> GetSpecificCanary(
        [FromKeyedServices("canaryCache")] VersionCache vcache,
        string version
    )
    {
        if (vcache[version] is { } cacheEntry)
            return Ok(cacheEntry);

        return NotFound();
    }
}