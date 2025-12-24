using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Ryujinx.Systems.Update.Common;
using Ryujinx.Systems.Update.Server.Services.GitLab;

namespace Ryujinx.Systems.Update.Server.Controllers;

[Route($"{Constants.FullRouteName_Api_Meta}")]
[ApiController]
public class MetaController : ControllerBase
{
    private static readonly CacheSourceMapping CacheSources = new()
    {
        Stable = VersionCacheSource.Empty,
        Canary = null
    };

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json")]
    [EndpointDescription("Query the UpdateServer's internal version caches to determine what GitLab projects back each release channel.")]
    public ActionResult<CacheSourceMapping> Action(
        [FromKeyedServices("stableCache")] VersionCache stableCache,
        [FromKeyedServices("canaryCache")] VersionCache canaryCache)
    {
        if (!stableCache.HasProjectInfo)
            return BadRequest("Stable cache isn't initialized yet.");

        CacheSources.Stable = new VersionCacheSource
        {
            Id = stableCache.ProjectId,
            Owner = stableCache.ProjectPath.Split('/')[0],
            Project = stableCache.ProjectPath.Split('/')[1]
        };

        if (canaryCache.HasProjectInfo)
        {
            CacheSources.Canary = new VersionCacheSource
            {
                Id = canaryCache.ProjectId,
                Owner = canaryCache.ProjectPath.Split('/')[0],
                Project = canaryCache.ProjectPath.Split('/')[1]
            };
        }

        return Ok(CacheSources);
    }
}