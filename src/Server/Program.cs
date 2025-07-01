using Microsoft.AspNetCore.HttpOverrides;
using Ryujinx.Systems.Update.Server;
using Ryujinx.Systems.Update.Server.Helpers;
using Ryujinx.Systems.Update.Server.Services;
using Ryujinx.Systems.Update.Server.Services.GitLab;

CommandLineState.Init(args);

var builder = WebApplication.CreateBuilder(args);

if (CommandLineState.ListenPort != null)
    builder.WebHost.ConfigureKestrel(options => options.ListenLocalhost(CommandLineState.ListenPort.Value));

if (CommandLineState.UseHttpLogging)
    builder.Services.AddHttpLogging();

builder.Services.Configure<ForwardedHeadersOptions>(opts =>
{
    opts.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddSingleton<DefaultHttpClientProxy>();
builder.Services.AddSingleton<GitLabService>();
builder.Services.AddKeyedSingleton<VersionCache>("stableCache");
builder.Services.AddKeyedSingleton<VersionCache>("canaryCache");

Swagger.TrySetup(builder);

builder.Services.AddControllers();

var app = builder.Build();

app.UseForwardedHeaders();

Swagger.TryMapUi(app);

VersionCache.InitializeVersionCaches(app);

app.MapControllers();

if (CommandLineState.UseHttpLogging)
    app.UseHttpLogging();

TaskScheduler.UnobservedTaskException += (sender, eventArgs) =>
#pragma warning disable CA2254
    app.Logger.LogError(eventArgs.Exception.InnerException ?? eventArgs.Exception, null);
#pragma warning restore CA2254

var adminSec = app.Configuration.GetSection("Admin");
if (adminSec.Exists())
    AdminEndpointMetadata.Set(adminSec.GetValue<string>("AccessToken"));

app.Run();