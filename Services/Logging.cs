using Serilog;
using Microsoft.Extensions.Logging;
namespace telbot.Services;
public class Logger : IDisposable
{
  private static ILoggerFactory _loggerFactory;
  private static Microsoft.Extensions.Logging.ILogger _instance;
  private static readonly Object _lock = new();
  private bool _disposed = false; // To detect redundant calls
  private Logger() {}
  public static Microsoft.Extensions.Logging.ILogger GetInstance<T>()
  {
    lock (_lock)
    {
      if (_instance == null)
      {
        // Initialize Serilog and attach to Microsoft.Extensions.Logging
        var logfile_template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} | {Level:u} | {SourceContext} | {Message}{NewLine}{Exception}";
        var console_template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} | {Level:u} | {SourceContext} | {Message}";
        // Ensure the logger is flushed
        Serilog.Log.CloseAndFlush();
        var logger = new Serilog.LoggerConfiguration()
          .WriteTo.Console(
            outputTemplate: logfile_template,
            restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information
            )
          .WriteTo.File(
            path: "chatbot.log",
            outputTemplate: console_template,
            rollingInterval: RollingInterval.Day,
            restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose
            )
          .CreateLogger();
        // Use Serilog as the logging provider
        _loggerFactory = LoggerFactory.Create(builder =>
        {
          builder.AddSerilog(logger);
        });
      } 
      // Create a logger for the calling type T
      _instance = _loggerFactory.CreateLogger<T>();
      return _instance;
    }
  }
  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }
  protected virtual void Dispose(bool disposing)
  {
    if (!_disposed)
    {
      if (disposing)
      {
        // Dispose managed resources here.
        Log.CloseAndFlush();
        _loggerFactory?.Dispose();
      }
      // Dispose unmanaged resources here.
      _disposed = true;
      _instance = null; // Clear the singleton instance
      _loggerFactory = null;
    }
  }
  ~Logger()
  {
    // Finalizer (optional)
    Dispose(false);
  }
}