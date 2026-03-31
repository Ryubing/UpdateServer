using Ryujinx.Systems.Update.Common;
using Ryujinx.Systems.Update.Server.Services.Forgejo;

namespace Ryujinx.Systems.Update.Server;

public static class ServiceProviderExtensions
{
    public static ForgejoVersionCache GetCacheFor(this IServiceProvider serviceProvider, ReleaseChannel rc) 
        => serviceProvider.GetRequiredKeyedService<ForgejoVersionCache>($"{rc.QueryStringValue}Cache");
}