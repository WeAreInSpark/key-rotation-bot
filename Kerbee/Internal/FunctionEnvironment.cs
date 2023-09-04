using System;

namespace Kerbee.Internal;

internal static class FunctionEnvironment
{
    public static bool IsAvailable => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HOME"));

    public static string RootPath => IsAvailable ? Environment.ExpandEnvironmentVariables("%HOME%/site/wwwroot") : Environment.CurrentDirectory;
}
