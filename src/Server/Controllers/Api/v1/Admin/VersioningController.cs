using Microsoft.AspNetCore.Mvc;
using Ryujinx.Systems.Update.Common;
using Ryujinx.Systems.Update.Server.Services;

namespace Ryujinx.Systems.Update.Server.Controllers.Admin;

[Route(Constants.FullRouteName_Api_Versioning)]
[ApiController]
public class VersioningController : Controller
{
    private readonly ILogger<VersioningController> _logger;

    public VersioningController(ILogger<VersioningController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Constants.RouteName_Api_Versioning_GetNextVersion)]
    [EndpointDescription("Query the next Ryubing version from the version provider.")]
    public ActionResult<string> GetNext([FromQuery] string rc, [FromQuery] bool major = false)
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

        return Ok(versionProviderService.GetNextVersion(releaseChannel, major));
    }

    [HttpGet(Constants.RouteName_Api_Versioning_GetCurrentVersion)]
    [EndpointDescription("Query the current Ryubing version from the version provider.")]
    public ActionResult<string> GetCurrent([FromQuery] string rc)
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

        return Ok(versionProviderService.GetCurrentVersion(releaseChannel));
    }

    [HttpPatch(Constants.RouteName_Api_Versioning_IncrementVersion)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status418ImATeapot)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [EndpointDescription("Increment the current Ryubing version in the version provider for the given release channel." +
                         "Requires Admin Token authentication via 'Authorization' header.")]
    public ActionResult Increment([FromQuery] string rc, [FromHeader(Name = "Authorization")] string adminAccessToken)
    {
        if (!AdminEndpointMetadata.Enabled)
            return Problem("This instance of Ryubing UpdateServer is not configured to support this endpoint.",
                statusCode: 418);

        if (!rc.TryParseAsReleaseChannel(out var releaseChannel))
            return Problem(
                $"Unknown release channel '{rc}'; valid are '{Constants.StableRoute}' and '{Constants.CanaryRoute}'",
                statusCode: 404);

        if (!AdminEndpointMetadata.AccessToken.Equals(adminAccessToken))
            return Unauthorized();

        var versionProviderService = HttpContext.RequestServices
            .GetService<VersionProviderService>();

        if (versionProviderService is null)
            return Problem("This instance of Ryubing UpdateServer is not configured to support this endpoint.",
                statusCode: 418);

        var newVersion = versionProviderService.IncrementBuild(releaseChannel);

        _logger.LogInformation("{channel} incremented to: {newVersion}", releaseChannel, newVersion);

        return Ok();
    }

    [HttpPatch(Constants.RouteName_Api_Versioning_AdvanceVersion)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status418ImATeapot)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [EndpointDescription("Increases the major version number for the Ryubing version provider for both release channels." +
                         "Requires Admin Token authentication via 'Authorization' header.")]
    public ActionResult Advance([FromHeader(Name = "Authorization")] string adminAccessToken)
    {
        if (!AdminEndpointMetadata.Enabled)
            return Problem("This instance of Ryubing UpdateServer is not configured to support this endpoint.",
                statusCode: 418);


        if (!AdminEndpointMetadata.AccessToken.Equals(adminAccessToken))
            return Unauthorized();

        var versionProviderService = HttpContext.RequestServices
            .GetService<VersionProviderService>();

        if (versionProviderService is null)
            return Problem("This instance of Ryubing UpdateServer is not configured to support this endpoint.",
                statusCode: 418);

        versionProviderService.Advance();

        _logger.LogInformation("Advanced major version to 1.{major}.", versionProviderService.CurrentMajor);

        return Ok();
    }
}