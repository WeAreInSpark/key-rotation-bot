using System.Reflection;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace Kerbee.Internal;

internal class ApplicationVersionInitializer<TStartup> : ITelemetryInitializer
{
    public ApplicationVersionInitializer() => ApplicationVersion = typeof(TStartup).Assembly
                                             .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                             ?.InformationalVersion ?? "1.0.0";

    public string ApplicationVersion { get; }

    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Component.Version = ApplicationVersion;
    }
}
