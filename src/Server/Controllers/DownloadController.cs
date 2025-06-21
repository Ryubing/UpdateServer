using Gommon;
using Microsoft.AspNetCore.Mvc;
using Ryujinx.Systems.Update.Common;
using Ryujinx.Systems.Update.Server.Services.GitLab;

namespace Ryujinx.Systems.Update.Server.Controllers;

[Route(Constants.RouteName_Download)]
[ApiController]
public class DownloadController : ControllerBase
{
    [HttpGet(Constants.QueryRoute)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> DownloadCustom(
        [FromQuery] string os,
        [FromQuery] string arch,
        [FromQuery] string rc = Constants.StableRoute,
        [FromQuery] string version = Constants.RouteName_Latest
        )
    {
        if (os == string.Empty)
            return BadRequest("os was empty.");
        
        if (arch == string.Empty)
            return BadRequest("arch was empty.");

        if (rc == string.Empty)
            return BadRequest("rc was empty.");
        
        if (!os.TryParseAsSupportedPlatform(out var supportedPlatform))
            return BadRequest($"Unknown platform '{os}'");
        
        if (!arch.TryParseAsSupportedArchitecture(out var supportedArch))
            return BadRequest($"Unknown architecture '{arch}'");
        
        if (!rc.TryParseAsReleaseChannel(out var releaseChannel))
            return BadRequest($"Unknown release channel '{rc}'; valid are '{Constants.StableRoute}' and '{Constants.CanaryRoute}'");
        
        var release = await HttpContext.RequestServices
            .GetCacheFor(releaseChannel)
            .GetReleaseAsync(c => 
                version is Constants.RouteName_Latest ? c.Latest : c[version]
            );
        
        if (release is null)
            return NotFound();
        
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
        var latest = await vcache.GetReleaseAsync(c => c.Latest);

        if (latest is null)
            return NotFound();
        
        var uaString = HttpContext.Request.Headers.UserAgent.ToString();
        
        if (uaString.ContainsIgnoreCase("Mac"))
            return Redirect(latest.Downloads.MacOS);

        var platform = uaString.ContainsIgnoreCase("Linux") 
            ? latest.Downloads.Linux 
            : latest.Downloads.Windows;
        
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
        var latest = await vcache.GetReleaseAsync(c => c.Latest);

        if (latest is null)
            return NotFound();
        
        var uaString = HttpContext.Request.Headers.UserAgent.ToString();
        
        if (uaString.ContainsIgnoreCase("Mac"))
            return Redirect(latest.Downloads.MacOS);

        var platform = uaString.ContainsIgnoreCase("Linux") 
            ? latest.Downloads.Linux 
            : latest.Downloads.Windows;
        
        return Redirect(uaString.ContainsIgnoreCase("x64")
            ? platform.X64
            : platform.Arm64);
    }
}