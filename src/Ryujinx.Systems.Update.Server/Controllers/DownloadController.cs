using Gommon;
using Microsoft.AspNetCore.Mvc;
using Ryujinx.Systems.Updater.Common;
using Ryujinx.Systems.Updater.Server.Services.GitLab;

namespace Ryujinx.Systems.Updater.Server.Controllers;

[Route("[controller]")]
[ApiController]
public class DownloadController : ControllerBase
{
    [HttpGet("query")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> DownloadLatestCustom(
        [FromQuery] string os,
        [FromQuery] string arch,
        [FromQuery] string rc = "stable",
        [FromQuery] string version = "latest"
        )
    {
        if (rc is not ("stable" or "canary"))
            return BadRequest($"Unknown release channel '{rc}'; valid are 'stable' and 'canary'");

        var versionCache = HttpContext.RequestServices.GetRequiredKeyedService<VersionCache>(rc is "stable"
            ? "stableCache"
            : "canaryCache");
        
        var lck = await versionCache.TakeLockAsync();

        var release = version is "latest" ? versionCache.Latest : versionCache[version];
        
        lck.Dispose();
        
        if (release is null)
            return NotFound();

        if (os is "mac" or "osx" or "macos")
            return Redirect(release.Downloads.MacOS);
        
        var platform = os.ToLower() switch
        {
            "win" or "w" or "windows" => release.Downloads.Windows,
            "lin" or "l" or "linux" => release.Downloads.Linux,
            "ai" or "appimage" or "linuxappimage" or "linuxai" => release.Downloads.LinuxAppImage,
            _ => null
        };

        if (platform is null)
            return BadRequest($"Unknown platform '{os}'");

        var url = arch.ToLower() switch
        {
            "arm64" or "a64" or "arm" => platform.Arm64,
            "x64" or "x86-64" or "x86_64" or "amd64" => platform.X64,
            _ => null
        };
        
        if (url is null)
            return BadRequest($"Unknown architecture '{arch}'");
        
        return Redirect(url);
    }
    
    [HttpGet]
    [HttpGet("stable")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> DownloadLatestStable(
        [FromKeyedServices("stableCache")] VersionCache vcache
    )
    {
        var lck = await vcache.TakeLockAsync();
        
        var latest = vcache.Latest;
        
        lck.Dispose();

        if (latest is null)
            return NotFound();
        
        var uaString = HttpContext.Request.Headers.UserAgent.ToString();
        
        if (uaString.ContainsIgnoreCase("Mac"))
            return Redirect(latest.Downloads.MacOS);

        DownloadLinks.SupportedPlatform platform = latest.Downloads.Windows;
        
        if (uaString.ContainsIgnoreCase("Linux"))
            platform = latest.Downloads.Linux;
        
        return Redirect(uaString.ContainsIgnoreCase("x64")
            ? platform.X64
            : platform.Arm64);
    }
    
    [HttpGet("canary")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> DownloadLatestCanary(
        [FromKeyedServices("canaryCache")] VersionCache vcache
    )
    {
        var lck = await vcache.TakeLockAsync();
        
        var latest = vcache.Latest;
        
        lck.Dispose();

        if (latest is null)
            return NotFound();
        
        var uaString = HttpContext.Request.Headers.UserAgent.ToString();
        
        if (uaString.ContainsIgnoreCase("Mac"))
            return Redirect(latest.Downloads.MacOS);

        DownloadLinks.SupportedPlatform platform = latest.Downloads.Windows;
        
        if (uaString.ContainsIgnoreCase("Linux"))
            platform = latest.Downloads.Linux;
        
        return Redirect(uaString.ContainsIgnoreCase("x64")
            ? platform.X64
            : platform.Arm64);
    }
}