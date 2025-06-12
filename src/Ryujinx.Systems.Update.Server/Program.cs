using Gommon;
using NGitLab.Models;
using NSwag;
using Ryujinx.Systems.Updater.Server.Services.GitLab;

const string apiVersion = "v1";

var useHttpLogging = false;
var useSwagger = false;
int? listenToPort = null;

foreach (var (index, arg) in args.Index())
{
    switch (arg.ToLower())
    {
        case "--port":
        case "-p":
        {
            if (index + 1 >= args.Length)
                throw new Exception("port argument expects a value");

            if (!int.TryParse(args[index + 1], out var port))
                throw new Exception("port argument must be an integer");

            listenToPort = port;
            
            break;
        }
        case "--http-logging":
        case "-l":
        {
            useHttpLogging = true;
            break;
        }
        case "--enable-swagger":
        case "-s":
        {
            useSwagger = true;
            break;
        }
    }
}

var builder = WebApplication.CreateBuilder(args);

if (listenToPort != null)
    builder.WebHost.ConfigureKestrel(options => options.ListenLocalhost(listenToPort.Value));

if (useHttpLogging)
    builder.Services.AddHttpLogging();

builder.Services.AddSingleton<GitLabService>();
builder.Services.AddKeyedSingleton<VersionCache>("stableCache");
builder.Services.AddKeyedSingleton<VersionCache>("canaryCache");

if (useSwagger)
{
    builder.Services.AddOpenApi(apiVersion);
    builder.Services.AddOpenApiDocument(opt =>
    {
        opt.PostProcess = doc =>
        {
            doc.Info = new OpenApiInfo
            {
                Version = apiVersion,
                Title = "Ryujinx Updates",
                Description = "REST API for Ryubing updates powered by ASP.NET Core."
            };
        };
    });
}

builder.Services.AddControllers();

var app = builder.Build();

if (useSwagger)
{
    app.MapOpenApi();
    app.UseSwaggerUi(opt =>
    {
        opt.ServerUrl = app.Configuration["ServerUrl"];
        opt.DocumentPath = $"/openapi/{apiVersion}.json";
    });
}

var versionCacheSection = app.Configuration.GetSection("GitLab").GetRequiredSection("VersionCacheSources");

var stableSource = versionCacheSection.GetValue<string>("Stable");

if (stableSource is null)
    throw new Exception("Cannot start the server without a GitLab repository in GitLab:VersionCacheSources:Stable");

app.Services.GetRequiredKeyedService<VersionCache>("stableCache").Init(new ProjectId(stableSource));

var canarySource = versionCacheSection.GetValue<string>("Canary");

if (canarySource != null)
    app.Services.GetRequiredKeyedService<VersionCache>("canaryCache").Init(new ProjectId(canarySource));

app.MapControllers();

if (useHttpLogging)
    app.UseHttpLogging();

TaskScheduler.UnobservedTaskException += (sender, eventArgs) =>
#pragma warning disable CA2254
    app.Logger.LogError(eventArgs.Exception.InnerException ?? eventArgs.Exception, null);
#pragma warning restore CA2254

app.Run();