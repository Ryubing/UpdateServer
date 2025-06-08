using Microsoft.AspNetCore.Mvc;
using RyujinxUpdate.Model;
using RyujinxUpdate.Services.GitLab;

namespace RyujinxUpdate.Controllers;

[Route("[controller]")]
[ApiController]
public class LatestController : ControllerBase
{
    [HttpGet("query")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> GetLatestCustom(
        [FromQuery] string? os,
        [FromQuery] string? arch,
        [FromQuery] string? rc = "stable"
    )
    {
        if (rc is not ("stable" or "canary"))
            return BadRequest($"Unknown release channel '{rc}'; valid are 'stable' and 'canary'");

        var versionCache = HttpContext.RequestServices.GetRequiredKeyedService<VersionCache>(rc is "stable"
            ? "stableCache"
            : "canaryCache");

        var lck = await versionCache.TakeLockAsync();
        
        var latest = versionCache.Latest;
        
        lck.Dispose();

        if (latest is null)
            return NotFound();

        if (os is "mac" or "osx" or "macos")
            return Ok(new VersionResponse
            {
                Version = latest.Tag,
                ArtifactUrl = latest.Downloads.MacOS,
                ReleaseUrl = versionCache.FormatReleaseUrl(latest.Tag)
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
                ReleaseUrl = versionCache.FormatReleaseUrl(latest.Tag)
            });

        return NotFound();
    }
}