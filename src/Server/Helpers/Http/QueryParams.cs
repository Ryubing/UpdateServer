namespace Ryujinx.Systems.Update.Server.Helpers.Http;

public static class QueryParams
{
    public static (string, object) Sort(string type) => ("sort", type);
    public static (string, object) OrderBy(string ordering) => ("order_by", ordering);
}