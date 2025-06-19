using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Ryujinx.Systems.Update.Common;
using Ryujinx.Systems.Update.Server;
using Ryujinx.Systems.Update.Server.Services.GitLab;

namespace Ryujinx.Systems.Update.Server.Controllers.Admin;

[Route(Constants.FullRouteName_Api_Admin_RefreshCache)]
[ApiController]
public class RefreshCacheController : ControllerBase
{
    internal static (bool EndpointEnabled, string AccessToken) Meta = (false, "");
    
    private static readonly Dictionary<ReleaseChannel, DateTimeOffset> LastRefreshes =
        new()
        {
            { ReleaseChannel.Stable, DateTimeOffset.MinValue },
            { ReleaseChannel.Canary, DateTimeOffset.MinValue }
        };

    [HttpPatch]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status418ImATeapot)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [SuppressMessage("ReSharper.DPA", "DPA0011: High execution time of MVC action")]
    public async Task<ActionResult<object>> Action([FromQuery] string rc)
    {
        if (!Meta.EndpointEnabled)
            return Problem("This instance of Ryubing UpdateServer is not configured to support this endpoint.",
                statusCode: 418);

        if (!rc.TryParseAsReleaseChannel(out var releaseChannel))
            return Problem("Unknown rc", statusCode: 404);

        if (!Meta.AccessToken.Equals(HttpContext.Request.Headers.Authorization.ToString(), StringComparison.Ordinal))
            return Unauthorized();
        
        var minutesSinceLastRefresh = (DateTimeOffset.Now - LastRefreshes[releaseChannel]).TotalMinutes;
        if (minutesSinceLastRefresh <= 1)
            return Problem("Try again later.", statusCode: 429);

        var vcache = HttpContext.RequestServices.GetCacheFor(releaseChannel);

        await vcache.Update();
        
        LastRefreshes[releaseChannel] = DateTimeOffset.Now;

        return Accepted();
    }
}