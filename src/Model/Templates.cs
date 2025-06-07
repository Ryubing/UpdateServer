namespace RyujinxUpdate.Model;

public static class Templates
{
    public static string CreateErrorJson(string message) =>
        $"{{\"message\":\"{message}\"}}";
}