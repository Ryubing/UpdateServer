using System.ComponentModel;
using Gommon;
using Microsoft.AspNetCore.Mvc;
using Ryujinx.Systems.Update.Common;
using Ryujinx.Systems.Update.Server.Helpers.Results;
using Ryujinx.Systems.Update.Server.Services.Forgejo;

namespace Ryujinx.Systems.Update.Server.Controllers;

[Route(Constants.RouteName_Latest)]
[ApiController]
public class LatestController : ControllerBase
{
    [HttpGet(Constants.QueryRoute)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EndpointDescription("Query the latest version of Ryubing.")]
    public async Task<ActionResult<VersionResponse>> GetLatestCustom(
        [FromQuery] string? os,
        [FromQuery] string? arch,
        [FromQuery] string? rc = Constants.StableRoute
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
            return BadRequest(
                $"Unknown release channel '{rc}'; valid are '{Constants.StableRoute}' and '{Constants.CanaryRoute}'");

        var vcache = HttpContext.RequestServices.GetCacheFor(releaseChannel);

        Return<VersionCacheEntry?> result = await vcache.GetReleaseAsync(c => c.GetLatest(supportedPlatform, supportedArch));
        
        if (result.IsOf(out ReleaseError error))
        {
            return error.Error switch
            {
                ReleaseError.ReleaseErrorType.NotInitialized => NotFound(),
                ReleaseError.ReleaseErrorType.RateLimited => Problem("The version cache has hit the request rate limit.", statusCode: StatusCodes.Status429TooManyRequests),
                _ => throw new InvalidEnumArgumentException()
            };
        }

        if (result.IsSuccess && result.Unwrap() is { } latest)
        {
            if (!Config.EnabledEndpoints.LatestQuery)
                return Ok(new VersionResponse
                {
                    Version = latest.Tag,
                    ArtifactUrl = "",
                    MaxConcurrency = Config.MaxConcurrentDownloads,
                    ReleaseUrlFormat = vcache.ReleaseUrlFormat
                });

            return Ok(new VersionResponse
            {
                Version = latest.Tag,
                ArtifactUrl = latest.GetUrlFor(supportedPlatform, supportedArch),
                MaxConcurrency = Config.MaxConcurrentDownloads,
                ReleaseUrlFormat = vcache.ReleaseUrlFormat
            });
        } 
        
        return NotFound();
    }

    [HttpGet(Constants.StableRoute), HttpGet]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [EndpointDescription("Redirect to the Forgejo release URL of the latest Stable Ryubing release.")]
    public async Task<ActionResult> RedirectLatestStable(
        [FromKeyedServices("stableCache")] ForgejoVersionCache vcache)
    {
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
            return Redirect(latest.ReleaseUrl);
        
        return NotFound();
    }

    [HttpGet(Constants.CanaryRoute)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointDescription("Redirect to the Forgejo release URL of the latest Canary Ryubing release.")]
    public async Task<ActionResult> RedirectLatestCanary(
        [FromKeyedServices("canaryCache")] ForgejoVersionCache vcache)
    {
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
            return Redirect(latest.ReleaseUrl);

        return NotFound();
    }
}