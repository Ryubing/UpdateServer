using System.Diagnostics.CodeAnalysis;
using Gommon;
using Microsoft.AspNetCore.Mvc;
using Ryujinx.Systems.Update.Common;

namespace Ryujinx.Systems.Update.Server.Controllers.Admin;

[Route(Constants.FullRouteName_Api_Admin_RefreshCache)]
[ApiController]
public class RefreshCacheController : ControllerBase
{
    private static readonly Dictionary<ReleaseChannel, DateTimeOffset> LastRefreshes =
        new()
        {
            { ReleaseChannel.Stable, DateTimeOffset.MinValue },
            { ReleaseChannel.Canary, DateTimeOffset.MinValue }
        };

    [HttpPatch]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status418ImATeapot)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [SuppressMessage("ReSharper.DPA", "DPA0011: High execution time of MVC action")]
    public async Task<ActionResult> Action([FromQuery] string rc)
    {
        if (!AdminEndpointMetadata.Enabled)
            return Problem("This instance of Ryubing UpdateServer is not configured to support this endpoint.",
                statusCode: 418);

        if (!rc.TryParseAsReleaseChannel(out var releaseChannel))
            return Problem(
                $"Unknown release channel '{rc}'; valid are '{Constants.StableRoute}' and '{Constants.CanaryRoute}'", statusCode: 404);

        if (!AdminEndpointMetadata.AccessToken.EqualsIgnoreCase(HttpContext.Request.Headers.Authorization))
            return Unauthorized();

        var minutesSinceLastRefresh = (DateTimeOffset.Now - LastRefreshes[releaseChannel]).TotalMinutes;
        if (minutesSinceLastRefresh <= 1)
            return Problem("Try again later.", statusCode: 429);

        await HttpContext.RequestServices.GetCacheFor(releaseChannel).RefreshAsync();

        LastRefreshes[releaseChannel] = DateTimeOffset.Now;

        return Accepted();
    }
}