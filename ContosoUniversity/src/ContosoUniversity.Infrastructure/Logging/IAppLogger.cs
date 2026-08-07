using System.Diagnostics;

namespace ContosoUniversity.Infrastructure.Logging;

/// <summary>
/// Logger interface for application logging.
/// Replaces the legacy ContosoUniversity.Logging.ILogger.
/// </summary>
public interface IAppLogger
{
    void Information(string message);
    void Information(string fmt, params object[] vars);
    void Warning(string message);
    void Warning(string fmt, params object[] vars);
    void Error(string message);
    void Error(Exception exception, string fmt, params object[] vars);
    void TraceApi(string componentName, string method, TimeSpan timespan, string properties = "");
}
