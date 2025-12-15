using Gommon;
using Microsoft.AspNetCore.Mvc;
using Ryujinx.Systems.Update.Common;
using Ryujinx.Systems.Update.Server.Services;

namespace Ryujinx.Systems.Update.Server.Controllers.Admin;

[Route(Constants.FullRouteName_Api_Versioning)]
[ApiController]
public class VersioningController : Controller
{
    [HttpGet(Constants.RouteName_Api_Versioning_GetNextVersion)]
    public async Task<ActionResult<string>> GetNext([FromQuery] string rc)
    {
        if (!rc.TryParseAsReleaseChannel(out var releaseChannel))
            return Problem(
                $"Unknown release channel '{rc}'; valid are '{Constants.StableRoute}' and '{Constants.CanaryRoute}'",
                statusCode: 404);

        var versionProviderService = HttpContext.RequestServices
            .GetService<VersionProviderService>();

        if (versionProviderService is null)
            return Problem("This instance of Ryubing UpdateServer is not configured to support this endpoint.",
                statusCode: 418);

        return Ok(versionProviderService.GetNextVersion(releaseChannel));
    }

    [HttpPatch(Constants.RouteName_Api_Versioning_IncrementVersion)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status418ImATeapot)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<ActionResult> Increment([FromQuery] string rc)
    {
        if (!AdminEndpointMetadata.Enabled)
            return Problem("This instance of Ryubing UpdateServer is not configured to support this endpoint.",
                statusCode: 418);

        if (!rc.TryParseAsReleaseChannel(out var releaseChannel))
            return Problem(
                $"Unknown release channel '{rc}'; valid are '{Constants.StableRoute}' and '{Constants.CanaryRoute}'",
                statusCode: 404);

        if (!AdminEndpointMetadata.AccessToken.EqualsIgnoreCase(HttpContext.Request.Headers.Authorization))
            return Unauthorized();

        var versionProviderService = HttpContext.RequestServices
            .GetService<VersionProviderService>();

        if (versionProviderService is null)
            return Problem("This instance of Ryubing UpdateServer is not configured to support this endpoint.",
                statusCode: 418);

        versionProviderService.IncrementBuild(releaseChannel);

        return Ok();
    }

    [HttpPatch(Constants.RouteName_Api_Versioning_AdvanceVersion)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status418ImATeapot)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<ActionResult> Advance()
    {
        if (!AdminEndpointMetadata.Enabled)
            return Problem("This instance of Ryubing UpdateServer is not configured to support this endpoint.",
                statusCode: 418);


        if (!AdminEndpointMetadata.AccessToken.EqualsIgnoreCase(HttpContext.Request.Headers.Authorization))
            return Unauthorized();

        var versionProviderService = HttpContext.RequestServices
            .GetService<VersionProviderService>();

        if (versionProviderService is null)
            return Problem("This instance of Ryubing UpdateServer is not configured to support this endpoint.",
                statusCode: 418);

        versionProviderService.Advance();

        return Ok();
    }
}