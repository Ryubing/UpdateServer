using System.Net;
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
    public async Task<ActionResult> DownloadCustom(
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
    public async Task<ActionResult> DownloadLatestStable(
        [FromKeyedServices("stableCache")] VersionCache vcache, 
        [FromServices] ILogger<DownloadController> logger
    )
    {
        if (await vcache.GetReleaseAsync(c => c.Latest) is not {} latest)
            return NotFound();
        
        return RedirectOrProblem(latest, logger, HttpContext.Request.Headers.UserAgent.ToString());
    }
    
    [HttpGet(Constants.CanaryRoute)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DownloadLatestCanary(
        [FromKeyedServices("canaryCache")] VersionCache vcache,
        [FromServices] ILogger<DownloadController> logger
    )
    {
        if (await vcache.GetReleaseAsync(c => c.Latest) is not {} latest)
            return NotFound();
        
        return RedirectOrProblem(latest, logger, HttpContext.Request.Headers.UserAgent.ToString());
    }
    
    private ActionResult RedirectOrProblem(VersionCacheEntry cacheEntry, ILogger<DownloadController> logger, string userAgent)
    {
        var (platform, arch) = VersionCacheEntry.GetVersionTupleForUserAgent(userAgent);
        
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        // I was still getting occasional errors for passing null into a Redirect, so idk
        if (cacheEntry.GetUrlFor(platform, arch) is not { } url)
        {
            logger.LogError(new EventId(1), "Requested download URL was null: Version: {ver}; User-Agent: '{userAgent}'", cacheEntry.Tag, userAgent);
            
            return Problem(statusCode: 500,
                detail: $"The requested download's url was null. Version: {cacheEntry.Tag}; User-Agent: '{userAgent}'");
        }
            
        return Redirect(url);
    }
}