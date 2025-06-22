namespace Ryujinx.Systems.Update.Server.Helpers;

public static class CommandLineState
{
#if DEBUG
    public static bool UseHttpLogging { get; private set; } = true;
    public static bool UseSwagger { get; private set; } = true;
#else
    public static bool UseHttpLogging { get; private set; } = false;
    public static bool UseSwagger { get; private set; } = false;
#endif
    public static int? ListenPort { get; private set; }

    public static void Init(string[] args)
    {
        foreach (var (index, arg) in args.Index())
        {
            switch (arg.ToLower())
            {
                case "--port":
                case "-p":
                {
                    if (index + 1 >= args.Length)
                        throw new Exception("port argument expects a value");

                    if (!args[index + 1].TryParse<int>(out var port))
                        throw new Exception("port argument must be an integer");

                    ListenPort = port;

                    break;
                }
                case "--http-logging":
                case "-l":
                {
                    UseHttpLogging = true;
                    break;
                }
                case "--enable-swagger":
                case "-s":
                {
                    UseSwagger = true;
                    break;
                }
            }
        }
    }
}