using System.Linq.Expressions;
using telbot.Interfaces;
using telbot.models;
namespace telbot.Services;
public class Database : IDatabase
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
        _instance = new SQLite(cfg); // TODO - Change to IDatabase that you want
      }
      return _instance;
    }
  }
  private Database(Configuration cfg)
  {
    if(RecuperarUsuario(cfg.ID_ADM_BOT) is null)
    {
      var proprietario = new UsersModel() {
        identifier = cfg.ID_ADM_BOT,
        create_at = DateTime.Now,
        update_at = DateTime.Now,
        privilege = UsersModel.userLevel.proprietario,
        inserted_by = cfg.ID_ADM_BOT,
        phone_number = 0
      };
      InserirUsuario(proprietario);
    }
  }
  public void InserirUsuario(UsersModel user_model)
  {
    _instance.InserirUsuario(user_model);
  }
  public List<UsersModel> RecuperarUsuario(Expression<Func<UsersModel, bool>>? expression = null) 
  {
    return _instance.RecuperarUsuario(expression);
  }
  public UsersModel? RecuperarUsuario(Int64 identifier)
  {
    return _instance.RecuperarUsuario(identifier);
  }
  public void AlterarUsuario(UsersModel user_model)
  {
    _instance.AlterarUsuario(user_model);
  }
  public void InserirSolicitacao(logsModel request)
  {
    _instance.InserirSolicitacao(request);
  }
  public List<logsModel> RecuperarSolicitacao(Expression<Func<logsModel, bool>>? expression = null)
  {
    return _instance.RecuperarSolicitacao(expression);
  }
  public logsModel? RecuperarSolicitacao(Int64 identifier)
  {
    return _instance.RecuperarSolicitacao(identifier);
  }
  public void AlterarSolicitacao(logsModel request)
  {
    _instance.AlterarSolicitacao(request);
  }
  public void InserirFatura(pdfsModel fatura)
  {
    _instance.InserirFatura(fatura);
  }
  public pdfsModel? RecuperarFatura(string filename)
  {
    return _instance.RecuperarFatura(filename);
  }
  public List<pdfsModel> RecuperarFatura(Expression<Func<pdfsModel, bool>>? expression = null)
  {
    return _instance.RecuperarFatura(expression);
  }
  public void AlterarFatura(pdfsModel fatura)
  {
    _instance.AlterarFatura(fatura);
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