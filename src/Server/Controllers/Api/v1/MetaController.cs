using Microsoft.AspNetCore.Mvc;
using Ryujinx.Systems.Update.Common;
using Ryujinx.Systems.Update.Server.Services.GitLab;

namespace Ryujinx.Systems.Update.Server.Controllers;

[Route($"{Constants.FullRouteName_Api_Meta}")]
[ApiController]
public class MetaController : ControllerBase
{
    private static readonly Dictionary<string, VersionCacheSource> CacheSources =
        new(2)
        {
            { "stable", VersionCacheSource.Empty },
            { "canary", VersionCacheSource.Empty }
        };
    
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json")]
    [EndpointDescription("Query the UpdateServer's internal version caches to determine what GitLab projects back each release channel.")]
    public ActionResult<Dictionary<string, VersionCacheSource>> Action()
    {
        var stableCache = HttpContext.RequestServices.GetRequiredKeyedService<VersionCache>("stableCache");

        if (!stableCache.HasProjectInfo)
            return BadRequest("Stable cache isn't initialized yet.");
        
        CacheSources["stable"] = new VersionCacheSource
        {
            Id = stableCache.ProjectId,
            Owner = stableCache.ProjectPath.Split('/')[0],
            Project = stableCache.ProjectPath.Split('/')[1]
        };
        
        var canaryCache = HttpContext.RequestServices.GetKeyedService<VersionCache>("canaryCache");

        if (canaryCache?.HasProjectInfo ?? false)
        {
            CacheSources["canary"] = new VersionCacheSource
            {
                Id = canaryCache.ProjectId,
                Owner = canaryCache.ProjectPath.Split('/')[0],
                Project = canaryCache.ProjectPath.Split('/')[1]
            };
        }

        return Ok(CacheSources);
    }
}