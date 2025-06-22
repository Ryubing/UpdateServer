using Gommon;

namespace Ryujinx.Systems.Update.Server.Controllers.Admin;

internal static class AdminEndpointMetadata
{
    public static bool Enabled { get; private set; }
    public static string AccessToken { get; private set; } = string.Empty;

    public static void Set(string? accessToken)
    {
        Enabled = !accessToken.IsNullOrEmpty();

        AccessToken = Enabled ? accessToken! : string.Empty;
    }
}