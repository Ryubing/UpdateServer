using NSwag;
using Ryujinx.Systems.Update.Common;
using Ryujinx.Systems.Update.Server.Helpers;

namespace Ryujinx.Systems.Update.Server;

public static class Swagger
{
    public static void TrySetup(WebApplicationBuilder builder)
    {
        if (CommandLineState.Swagger)
        {
            builder.Services.AddOpenApi(Constants.CurrentApiVersion);
            builder.Services.AddOpenApiDocument(opt =>
            {
                opt.PostProcess = doc =>
                {
                    doc.Info = new OpenApiInfo
                    {
                        Version = Constants.CurrentApiVersion,
                        Title = "Ryujinx Updates",
                        Description = "REST API for Ryubing updates powered by ASP.NET Core."
                    };
                };
            });
        }
    }

    public static void TryMapUi(WebApplication app)
    {
        if (CommandLineState.Swagger)
        {
            app.MapOpenApi();
            app.UseSwaggerUi(opt =>
            {
                opt.DocumentPath = $"/openapi/{Constants.CurrentApiVersion}.json";
            });
        }
    }
}