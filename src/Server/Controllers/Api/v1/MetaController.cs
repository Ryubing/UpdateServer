using Microsoft.AspNetCore.Mvc;
using Ryujinx.Systems.Update.Common;
using Ryujinx.Systems.Update.Server.Services.Forgejo;

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
    [EndpointDescription("Query the UpdateServer's internal version caches to determine what Forgejo  projects back each release channel.")]
    public ActionResult<CacheSourceMapping> Action(
        [FromKeyedServices("stableCache")] ForgejoVersionCache stableCache,
        [FromKeyedServices("canaryCache")] ForgejoVersionCache canaryCache)
    {
        if (!Config.EnabledEndpoints.VersionCacheMeta)
            return Problem("This instance of Ryubing UpdateServer is not configured to support this endpoint.",
                statusCode: 418);
        
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