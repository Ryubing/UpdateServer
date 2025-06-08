using System.Configuration;
using Gommon;
using NGitLab.Models;
using NSwag;
using RyujinxUpdate.Services.GitLab;

const string apiVersion = "v1";

var builder = WebApplication.CreateBuilder(args);

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

            builder.WebHost.ConfigureKestrel(options => options.ListenLocalhost(port));
            
            break;
        }
    }
}

var disableSwagger = args.ContainsIgnoreCase("--disable-swagger") || args.ContainsIgnoreCase("-ds");

builder.Services.AddSingleton<GitLabService>();
builder.Services.AddKeyedSingleton<VersionCache>("stableCache");
builder.Services.AddKeyedSingleton<VersionCache>("canaryCache");

if (!disableSwagger)
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

if (!disableSwagger)
{
    app.MapOpenApi();
    app.UseSwaggerUi(opt => opt.DocumentPath = $"/openapi/{apiVersion}.json");
}

var versionCacheSection = app.Configuration.GetSection("GitLab").GetRequiredSection("VersionCacheSources");

var stableSource = versionCacheSection.GetValue<string>("Stable");

if (stableSource is null)
    throw new ConfigurationErrorsException("Cannot start the server without a GitLab repository in GitLab:VersionCacheSources:Stable");

app.Services.GetRequiredKeyedService<VersionCache>("stableCache").Init(new ProjectId(stableSource));

var canarySource = versionCacheSection.GetValue<string>("Canary");

if (canarySource != null)
    app.Services.GetRequiredKeyedService<VersionCache>("canaryCache").Init(new ProjectId(canarySource));

app.MapControllers();

TaskScheduler.UnobservedTaskException += (sender, eventArgs) =>
#pragma warning disable CA2254
    app.Logger.LogError(eventArgs.Exception.InnerException ?? eventArgs.Exception, null);
#pragma warning restore CA2254

app.Run();