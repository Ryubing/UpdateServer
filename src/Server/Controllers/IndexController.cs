using Microsoft.AspNetCore.Mvc;
using Ryujinx.Systems.Update.Server.Helpers;

namespace Ryujinx.Systems.Update.Server.Controllers;

[Route("/")]
[ApiController]
public class IndexController : ControllerBase
{
    [HttpGet]
    public ActionResult Index() 
        => Redirect("https://github.com/Ryubing/UpdateServer/");

    [HttpGet("docs"), HttpGet("info"), HttpGet("help")]
    public ActionResult Help()
    {
        return CommandLineState.UseSwagger 
            ? Redirect("/swagger") 
            : Index();
    }
}