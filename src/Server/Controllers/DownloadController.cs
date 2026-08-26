using System.ComponentModel;
using Gommon;
using Microsoft.AspNetCore.Mvc;
using Ryujinx.Systems.Update.Common;
using Ryujinx.Systems.Update.Server.Helpers.Results;
using Ryujinx.Systems.Update.Server.Services.Forgejo;

namespace Ryujinx.Systems.Update.Server.Controllers;

[Route(Constants.RouteName_Download)]
[ApiController]
public class DownloadController : ControllerBase
{
    [HttpGet(Constants.QueryRoute)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EndpointDescription("Download a version of Ryubing.")]
    public async Task<ActionResult> DownloadCustom(
        [FromQuery] string os,
        [FromQuery] string arch,
        [FromQuery] string rc = Constants.StableRoute,
        [FromQuery] string version = Constants.RouteName_Latest
    )
    {
        if (!Config.EnabledEndpoints.DirectDownload)
            return Problem("This instance of Ryubing UpdateServer is not configured to support this endpoint.",
                statusCode: 418);

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
            return BadRequest(
                $"Unknown release channel '{rc}'; valid are '{Constants.StableRoute}' and '{Constants.CanaryRoute}'");

        var result = await HttpContext.RequestServices
            .GetCacheFor(releaseChannel)
            .GetReleaseAsync(c =>
                version is Constants.RouteName_Latest ? c.GetLatest(supportedPlatform, supportedArch) : c[version]
            );

        if (result.IsOf(out ReleaseError error))
        {
            return error.Error switch
            {
                ReleaseError.ReleaseErrorType.NotInitialized => NotFound(),
                ReleaseError.ReleaseErrorType.RateLimited => Problem("The version cache has hit the request rate limit.", statusCode: StatusCodes.Status429TooManyRequests),
                _ => throw new InvalidEnumArgumentException()
            };
        }
        
        if (result.IsSuccess && result.Unwrap() is {} release)
            return Redirect(release.GetUrlFor(supportedPlatform, supportedArch));

        return NotFound();
    }

    [HttpGet]
    [HttpGet(Constants.StableRoute)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointDescription("Download the latest Stable version of Ryubing.")]
    public async Task<ActionResult> DownloadLatestStable(
        [FromKeyedServices("stableCache")] ForgejoVersionCache vcache,
        [FromServices] ILogger<DownloadController> logger
    )
    {
        if (!Config.EnabledEndpoints.DirectDownload)
            return Problem("This instance of Ryubing UpdateServer is not configured to support this endpoint.",
                statusCode: 418);

        Return<VersionCacheEntry?> result = await vcache.GetReleaseAsync(c => c.Latest);

        if (result.IsOf(out ReleaseError error))
        {
            return error.Error switch
            {
                ReleaseError.ReleaseErrorType.NotInitialized => NotFound(),
                ReleaseError.ReleaseErrorType.RateLimited => Problem("The version cache has hit the request rate limit.", statusCode: StatusCodes.Status429TooManyRequests),
                _ => throw new InvalidEnumArgumentException()
            };
        }
        
        if (result.IsSuccess && result.Unwrap() is {} latest)
            return RedirectOrProblem(latest, logger, HttpContext.Request.Headers.UserAgent.ToString());
        
        return NotFound();
    }

    [HttpGet(Constants.CanaryRoute)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointDescription("Download the latest Canary version of Ryubing.")]
    public async Task<ActionResult> DownloadLatestCanary(
        [FromKeyedServices("canaryCache")] ForgejoVersionCache vcache,
        [FromServices] ILogger<DownloadController> logger
    )
    {
        if (!Config.EnabledEndpoints.DirectDownload)
            return Problem("This instance of Ryubing UpdateServer is not configured to support this endpoint.",
                statusCode: 418);

        Return<VersionCacheEntry?> result = await vcache.GetReleaseAsync(c => c.Latest);

        if (result.IsOf(out ReleaseError error))
        {
            return error.Error switch
            {
                ReleaseError.ReleaseErrorType.NotInitialized => NotFound(),
                ReleaseError.ReleaseErrorType.RateLimited => Problem("The version cache has hit the request rate limit.", statusCode: StatusCodes.Status429TooManyRequests),
                _ => throw new InvalidEnumArgumentException()
            };
        }
        
        if (result.IsSuccess && result.Unwrap() is {} latest)
            return RedirectOrProblem(latest, logger, HttpContext.Request.Headers.UserAgent.ToString());
        
        return NotFound();
    }

    private ActionResult RedirectOrProblem(VersionCacheEntry cacheEntry, ILogger<DownloadController> logger,
        string userAgent)
    {
        var (platform, arch) = VersionCacheEntry.GetVersionTupleForUserAgent(userAgent);

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        // I was still getting occasional errors for passing null into a Redirect, so idk
        if (cacheEntry.GetUrlFor(platform, arch) is not { } url)
        {
            logger.LogError(new EventId(1),
                "Requested download URL was null: Version: {ver}; User-Agent: '{userAgent}'", cacheEntry.Tag,
                userAgent);

            return Problem(statusCode: 500,
                detail: $"The requested download's url was null. Version: {cacheEntry.Tag}; User-Agent: '{userAgent}'");
        }

        return Redirect(url);
    }
}