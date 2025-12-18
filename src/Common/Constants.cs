// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global
namespace Ryujinx.Systems.Update.Common;

internal static class Constants
{
    public const string CurrentApiVersion = API_v1;
    public const string API_v1 = "v1";
    public const string API_Prefix = "api";

    public const string FullApiPrefix = $"{API_Prefix}/{CurrentApiVersion}/";

    public const string QueryRoute = "query";

    public const string StableRoute = "stable";
    public const string CanaryRoute = "canary";

    public const string RouteName_Download = "download";
    public const string RouteName_Latest = "latest";
    public const string RouteName_Api_Version = "version";
    public const string RouteName_Api_Versioning = "versioning";
    public const string RouteName_Api_Versioning_GetNextVersion = "next";
    public const string RouteName_Api_Versioning_GetCurrentVersion = "current";
    public const string RouteName_Api_Versioning_AdvanceVersion = "advance";
    public const string RouteName_Api_Versioning_IncrementVersion = "increment";
    public const string RouteName_Api_Meta = "meta";
    public const string RouteName_Api_Admin = "admin";
    public const string RouteName_Api_Admin_RefreshCache = "refresh_cache";
    
    public const string FullRouteName_Api_Meta = $"{FullApiPrefix}{RouteName_Api_Meta}";
    public const string FullRouteName_Api_Version = $"{FullApiPrefix}{RouteName_Api_Version}";
    public const string FullRouteName_Api_Versioning = $"{FullApiPrefix}{RouteName_Api_Versioning}";
    public const string FullRouteName_Api_Admin_RefreshCache = $"{FullApiPrefix}{RouteName_Api_Admin}/{RouteName_Api_Admin_RefreshCache}";
}