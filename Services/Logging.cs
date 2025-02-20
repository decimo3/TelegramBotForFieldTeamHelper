using Serilog;
using Serilog.Extensions.Logging;
using Microsoft.Extensions.Logging;
namespace telbot.Services;
public static class Logger
{
  private static readonly Lazy<ILoggerFactory> _loggerFactory = new(() => {
    var logsfilepath = System.IO.Path.Combine(AppContext.BaseDirectory, "log", "telbot_.log");
    var console_template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} | {Level:u} | {SourceContext} | {Message}{NewLine}";
    var logfile_template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} | {Level:u} | {SourceContext} | {Message} {Exception}{NewLine}";
    Log.Logger = new LoggerConfiguration() // Configure Serilog directly
      .MinimumLevel.Verbose()
      .WriteTo.Console(
        outputTemplate: console_template,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information
      )
      .WriteTo.File(
        path: logsfilepath,
        outputTemplate: logfile_template,
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose
      )
      .CreateLogger();
    return new SerilogLoggerFactory(Log.Logger); // Crucial step! // Return the SerilogLoggerFactory directly
  });
  public static Microsoft.Extensions.Logging.ILogger GetInstance<T>()
  {
    return _loggerFactory.Value.CreateLogger<T>();
  }
  public static void CloseAndFlush()
  {
    Log.CloseAndFlush();
  }
}
