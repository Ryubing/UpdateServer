using Gommon;
using Microsoft.AspNetCore.Mvc;
using Ryujinx.Systems.Update.Common;
using Ryujinx.Systems.Update.Server;
using Ryujinx.Systems.Updater.Common;
using Ryujinx.Systems.Updater.Server.Services.GitLab;

namespace Ryujinx.Systems.Updater.Server.Controllers;

[Route(Constants.RouteName_Download)]
[ApiController]
public class DownloadController : ControllerBase
{
    [HttpGet(Constants.QueryRoute)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> DownloadLatestCustom(
        [FromQuery] string os,
        [FromQuery] string arch,
        [FromQuery] string rc = Constants.StableRoute,
        [FromQuery] string version = Constants.RouteName_Latest
        )
    {
        if (!rc.TryParseAsReleaseChannel(out var releaseChannel))
            return BadRequest($"Unknown release channel '{rc}'; valid are '{Constants.StableRoute}' and '{Constants.CanaryRoute}'");

        var versionCache = HttpContext.RequestServices.GetCacheFor(releaseChannel);
        
        var lck = await versionCache.TakeLockAsync();

        var release = version is Constants.RouteName_Latest ? versionCache.Latest : versionCache[version];
        
        lck.Dispose();
        
        if (release is null)
            return NotFound();
        
        if (!os.TryParseAsSupportedPlatform(out var supportedPlatform))
            return BadRequest($"Unknown platform '{os}'");
        
        if (!arch.TryParseAsSupportedArchitecture(out var supportedArch))
            return BadRequest($"Unknown architecture '{arch}'");
        
        return Redirect(release.GetUrlFor(supportedPlatform, supportedArch));
    }
    
    [HttpGet]
    [HttpGet(Constants.StableRoute)]
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
    
    [HttpGet(Constants.CanaryRoute)]
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