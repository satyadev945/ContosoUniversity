using Microsoft.Extensions.Logging;

namespace ContosoUniversity.Infrastructure.Logging;

/// <summary>
/// Logger implementation using Microsoft.Extensions.Logging.
/// Replaces the legacy Trace-based Logger class.
/// </summary>
public class AppLogger : IAppLogger
{
    private readonly ILogger<AppLogger> _logger;

    public AppLogger(ILogger<AppLogger> logger)
    {
        _logger = logger;
    }

    public void Information(string message)
    {
        _logger.LogInformation("{Message}", message);
    }

    public void Information(string fmt, params object[] vars)
    {
        _logger.LogInformation(fmt, vars);
    }

    public void Warning(string message)
    {
        _logger.LogWarning("{Message}", message);
    }

    public void Warning(string fmt, params object[] vars)
    {
        _logger.LogWarning(fmt, vars);
    }

    public void Error(string message)
    {
        _logger.LogError("{Message}", message);
    }

    public void Error(Exception exception, string fmt, params object[] vars)
    {
        _logger.LogError(exception, fmt, vars);
    }

    public void TraceApi(string componentName, string method, TimeSpan timespan, string properties = "")
    {
        _logger.LogInformation(
            "Component: {ComponentName}; Method: {Method}; Timespan: {Timespan}; Properties: {Properties}",
            componentName, method, timespan, properties);
    }
}
