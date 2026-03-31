using Microsoft.AspNetCore.HttpOverrides;
using Ryujinx.Systems.Update.Server;
using Ryujinx.Systems.Update.Server.Helpers;
using Ryujinx.Systems.Update.Server.Services;
using Ryujinx.Systems.Update.Server.Services.Forgejo;

if (!CommandLineState.Init(args))
    return;

var builder = WebApplication.CreateBuilder(args);

builder.TryUseVersionPinning();
builder.TryUseVersionProvider();

if (CommandLineState.Port != null)
    builder.WebHost.ConfigureKestrel(options => options.ListenLocalhost(CommandLineState.Port.Value));

if (CommandLineState.HttpLogging)
    builder.Services.AddHttpLogging();

builder.Services.Configure<ForwardedHeadersOptions>(opts =>
{
    opts.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddSingleton<DefaultHttpClientProxy>();
builder.Services.AddSingleton<ForgejoService>();
builder.Services.AddKeyedSingleton<ForgejoVersionCache>("stableCache");
builder.Services.AddKeyedSingleton<ForgejoVersionCache>("canaryCache");
builder.Services.AddKeyedSingleton<ForgejoVersionCache>("custom1Cache");
builder.Services.AddKeyedSingleton<ForgejoVersionCache>("kenjinxCache");

Swagger.TrySetup(builder);

builder.Services.AddControllers();

var app = builder.Build();

app.UseForwardedHeaders();

Swagger.TryMapUi(app);

ForgejoVersionCache.InitializeVersionCaches(app);

app.MapControllers();

if (CommandLineState.HttpLogging)
    app.UseHttpLogging();

TaskScheduler.UnobservedTaskException += (sender, eventArgs) =>
#pragma warning disable CA2254
    app.Logger.LogError(eventArgs.Exception.InnerException ?? eventArgs.Exception, null);
#pragma warning restore CA2254

var adminSec = app.Configuration.GetSection("Admin");
if (adminSec.Exists())
    AdminEndpointMetadata.Set(adminSec.GetValue<string>("AccessToken"));

app.Run();