using Microsoft.AspNetCore.Mvc;
using Ryujinx.Systems.Update.Common;
using Ryujinx.Systems.Update.Server.Services.GitLab;

namespace Ryujinx.Systems.Update.Server.Controllers;

[Route(Constants.RouteName_Latest)]
[ApiController]
public class LatestController : ControllerBase
{
    [HttpGet(Constants.QueryRoute)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
        
        if (await vcache.GetReleaseAsync(c => c.Latest) is not { } latest)
            return NotFound();

        return Ok(new VersionResponse
        {
            Version = latest.Tag,
            ArtifactUrl = latest.GetUrlFor(supportedPlatform, supportedArch),
            ReleaseUrlFormat = vcache.ReleaseUrlFormat
        });
    }

    [HttpGet(Constants.StableRoute), HttpGet]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RedirectLatestStable(
        [FromKeyedServices("stableCache")] VersionCache vcache)
    {
        if (await vcache.GetReleaseAsync(c => c.Latest) is { } latest)
            return Redirect(latest.ReleaseUrl);

        return NotFound();
    }

    [HttpGet(Constants.CanaryRoute)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RedirectLatestCanary(
        [FromKeyedServices("canaryCache")] VersionCache vcache)
    {
        if (await vcache.GetReleaseAsync(c => c.Latest) is { } latest)
            return Redirect(latest.ReleaseUrl);

        return NotFound();
    }
}