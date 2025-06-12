using Microsoft.AspNetCore.Mvc;
using Ryujinx.Systems.Updater.Common;
using Ryujinx.Systems.Updater.Server.Services.GitLab;

namespace Ryujinx.Systems.Updater.Server.Controllers.Admin;

[Route(Constants.FullRouteName_Api_Admin_RefreshCache)]
[ApiController]
public class RefreshCacheController : ControllerBase
{
    private static (DateTimeOffset? Stable, DateTimeOffset? Canary) _lastManualRefresh;

    [HttpPatch]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status418ImATeapot)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<ActionResult<object>> Action(
        [FromServices] IConfiguration instanceConfiguration,
        [FromQuery] string rc)
    {
        var sec = instanceConfiguration.GetSection("Admin");
        if (!sec.Exists() || sec.GetValue<string>("AccessToken") is not { } accessToken || accessToken is "")
            return Problem("This instance of Ryubing UpdateServer is not configured to support this endpoint.",
                statusCode: 418);

        if (rc is not (Constants.StableRoute or Constants.CanaryRoute))
            return Problem("Unknown rc", statusCode: 404);

        var authHeader = HttpContext.Request.Headers.Authorization.ToString();

        if (!accessToken.Equals(authHeader, StringComparison.Ordinal))
            return Unauthorized();

        var lastRefresh = rc is Constants.StableRoute ? _lastManualRefresh.Stable : _lastManualRefresh.Canary;

        if (lastRefresh.HasValue)
        {
            var minutes = (DateTimeOffset.Now - lastRefresh.Value).TotalMinutes;
            if (minutes <= 1)
                return Problem("Try again later.", statusCode: 429);
        }

        var vcache = HttpContext.RequestServices.GetKeyedService<VersionCache>(
            rc is Constants.StableRoute ? "stableCache" : "canaryCache"
        )!;

        await vcache.Update();
        
        if (rc is Constants.StableRoute)
            _lastManualRefresh.Stable = DateTimeOffset.Now;
        else
            _lastManualRefresh.Canary = DateTimeOffset.Now;

        return Ok();
    }
}