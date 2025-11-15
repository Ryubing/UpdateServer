using Ryujinx.Systems.Update.Common;
using Ryujinx.Systems.Update.Server.Services.GitLab;

namespace Ryujinx.Systems.Update.Server;

public static class ServiceProviderExtensions
{
    public static VersionCache GetCacheFor(this IServiceProvider serviceProvider, ReleaseChannel rc) 
        => serviceProvider.GetRequiredKeyedService<VersionCache>($"{rc.QueryStringValue}Cache");
}