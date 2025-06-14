﻿using Microsoft.AspNetCore.Mvc;
using Ryujinx.Systems.Update.Common;
using Ryujinx.Systems.Updater.Common;
using Ryujinx.Systems.Updater.Server.Services.GitLab;

namespace Ryujinx.Systems.Updater.Server.Controllers;

[Route(Constants.RouteName_Latest)]
[ApiController]
public class LatestController : ControllerBase
{
    [HttpGet(Constants.QueryRoute)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> GetLatestCustom(
        [FromQuery] string? os,
        [FromQuery] string? arch,
        [FromQuery] string? rc = Constants.StableRoute
    )
    {
        if (!rc.TryParseAsReleaseChannel(out var releaseChannel))
            return BadRequest(
                $"Unknown release channel '{rc}'; valid are '{Constants.StableRoute}' and '{Constants.CanaryRoute}'");

        var versionCache =
            HttpContext.RequestServices.GetRequiredKeyedService<VersionCache>(
                $"{releaseChannel.AsQueryStringValue()}Cache");

        var lck = await versionCache.TakeLockAsync();

        var latest = versionCache.Latest;

        lck.Dispose();

        if (latest is null)
            return NotFound();

        if (!os.TryParseAsSupportedPlatform(out var supportedPlatform))
            return BadRequest($"Unknown platform '{os}'");

        if (!arch.TryParseAsSupportedArchitecture(out var supportedArch))
            return BadRequest($"Unknown architecture '{arch}'");

        return Ok(new VersionResponse
        {
            Version = latest.Tag,
            ArtifactUrl = latest.GetUrlFor(supportedPlatform, supportedArch),
            ReleaseUrl = versionCache.FormatReleaseUrl(latest.Tag)
        });
    }

    [HttpGet(Constants.StableRoute), HttpGet]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RedirectLatestStable(
        [FromKeyedServices("stableCache")] VersionCache vcache)
    {
        var lck = await vcache.TakeLockAsync();

        var latest = vcache.Latest;

        lck.Dispose();

        return latest is not null
            ? Redirect(latest.ReleaseUrl)
            : NotFound();
    }

    [HttpGet(Constants.CanaryRoute)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RedirectLatestCanary(
        [FromKeyedServices("canaryCache")] VersionCache vcache)
    {
        var lck = await vcache.TakeLockAsync();

        var latest = vcache.Latest;

        lck.Dispose();

        return latest is not null
            ? Redirect(latest.ReleaseUrl)
            : NotFound();
    }
}