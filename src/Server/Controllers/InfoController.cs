using Gommon;
using Microsoft.AspNetCore.Mvc;
using Ryujinx.Systems.Update.Server.Helpers;

namespace Ryujinx.Systems.Update.Server.Controllers;

[Route("/")]
[ApiController]
public class InfoController : ControllerBase
{
    [HttpGet]
    [ApiExplorerSettings(IgnoreApi = true)]
    public ActionResult Index() 
        => Redirect("https://github.com/Ryubing/UpdateServer/");

    [HttpGet("docs"), HttpGet("info"), HttpGet("help")]
    [EndpointDescription("Redirects the requestor to the /swagger endpoint.")]
    public ActionResult Help()
    {
        return CommandLineState.Swagger
            ? Redirect("/swagger") 
            : Index();
    }

    public static readonly string ServerVersion = typeof(Program).Assembly.GetName().Version!.ToString()[..^2];
    public const string ReleaseUrlFormat = "https://github.com/Ryubing/UpdateServer/releases/tag/{0}";

    [HttpGet("version")]
    [EndpointDescription("Redirects to or shows the requestor the current UpdateServer GitHub release URL.")]
    public ActionResult Version([FromQuery] bool redirect = true)
    {
        return redirect
            ? Redirect(ReleaseUrlFormat.Format(ServerVersion))
            : Ok(ReleaseUrlFormat.Format(ServerVersion));
    }
}