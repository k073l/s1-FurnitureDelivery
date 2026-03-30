using System.Diagnostics;
using System.Reflection;
using MelonLoader;
using System.Drawing;

namespace FurnitureDelivery.Helpers;

public static class MelonLoggerExtensions
{
    public static void Debug(this MelonLogger.Instance logger, string message, bool stacktrace = true)
    {
        MelonDebug.Msg(stacktrace ? $"[{GetCallerInfo()}] {message}" : message);
    }

    private static string GetCallerInfo()
    {
        var stackTrace = new StackTrace();
        for (int i = 2; i < stackTrace.FrameCount; i++)
        {
            var frame = stackTrace.GetFrame(i);
            var method = frame.GetMethod();
            if (method?.DeclaringType == null)
                continue;

            return $"{method.DeclaringType.FullName}.{method.Name}";
        }

        return "unknown";
    }
}