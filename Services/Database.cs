using telbot.Interfaces;
namespace telbot.Services;
public class Database
{
  private static IDatabase _instance;
  private static readonly Object _lock = new();
  private bool _disposed = false; // To detect redundant calls
  public static IDatabase GetInstance(Configuration? cfg = null)
  {
    lock (_lock)
    {
      if (_instance == null)
      {
        if (cfg == null)
        {
          throw new InvalidOperationException(
            "Database must be instantiated with a valid Configuration object.");
        }
        _instance = new PostgreSQL(cfg); // TODO - Change to IDatabase that you want
      }
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
        _instance.Dispose();
        // Dispose managed resources here.
      }
      // Dispose unmanaged resources here.
      _disposed = true;
      _instance = null; // Clear the singleton instance
    }
  }
  ~Database()
  {
    // Finalizer (optional)
    Dispose(false);
  }
}