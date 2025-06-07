using System.Configuration;
using Gommon;
using NGitLab.Models;
using NSwag;
using RyujinxUpdate.Services.GitLab;

const string ApiVersion = "v1";

var builder = WebApplication.CreateBuilder(args);

var disableSwagger = args.ContainsIgnoreCase("--disable-swagger") || args.ContainsIgnoreCase("-ds");

builder.Services.AddSingleton<GitLabService>();
builder.Services.AddKeyedSingleton<VersionCache>("stableCache");
builder.Services.AddKeyedSingleton<VersionCache>("canaryCache");

if (!disableSwagger)
{
    builder.Services.AddOpenApi(ApiVersion);
    builder.Services.AddOpenApiDocument(opt =>
    {
        opt.PostProcess = doc =>
        {
            doc.Info = new OpenApiInfo
            {
                Version = ApiVersion,
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
    app.UseSwaggerUi(opt => opt.DocumentPath = $"/openapi/{ApiVersion}.json");
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
    app.Logger.LogError(new EventId(-1, "TaskException"), eventArgs.Exception.Message, eventArgs.Exception);

app.Run();