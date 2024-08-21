using System.Data.SQLite;
using System.Linq.Expressions;
using telbot.Interfaces;
using telbot.models;
namespace telbot.Services;
public class Database : IDatabase
{
  private static Database _instance;
  private static readonly Object _lock = new object();
  private bool _disposed = false; // To detect redundant calls
  private readonly String connectionString = "Data Source=database.db";
  public static Database GetInstance(Configuration? cfg = null)
  {
    lock (_lock)
    {
      if (_instance == null)
      {
        if (cfg == null)
        {
          throw new InvalidOperationException("Database must be instantiated with a valid Configuration object.");
        }
        _instance = new Database(cfg);
      }
      return _instance;
    }
  }
  private Database(Configuration cfg)
  {
    if(!System.IO.File.Exists("database.db"))
    {
      SQLiteConnection.CreateFile("database.db");
    }
    using(var connection = new SQLiteConnection(connectionString))
    {
      connection.Open();
      using(var command = connection.CreateCommand())
      {
        command.CommandText = @$"CREATE TABLE IF NOT EXISTS usuarios(
            identifier INT PRIMARY KEY,
            create_at DATETIME NOT NULL,
            update_at DATETIME NOT NULL,
            privilege INT DEFAULT 0,
            inserted_by INT DEFAULT 0,
            phone_number INT DEFAULT 0
            )";
        command.ExecuteNonQuery();
        command.CommandText = @$"CREATE TABLE IF NOT EXISTS solicitacoes(
            identifier INT NOT NULL,
            application VARCHAR(16) NOT NULL,
            information INT NOT NULL,
            received_at DATETIME NOT NULL,
            response_at DATETIME DEFAULT NULL,
            status INT DEFAULT NULL,
            instance INT DEFAULT NULL
            )";
        command.ExecuteNonQuery();
      }
    }
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
    using(var connection = new SQLiteConnection(connectionString))
    {
      connection.Open();
      using(var command = connection.CreateCommand())
      {
        command.CommandText = "INSERT INTO usuarios" + 
          "(identifier, create_at, update_at, privilege, inserted_by, phone_number)" +
          "VALUES (@valor1, @valor2, @valor3, @valor4, @valor5, @valor6)";
        command.Parameters.Add(new SQLiteParameter("@valor1", user_model.identifier));
        command.Parameters.Add(new SQLiteParameter("@valor2", user_model.create_at.ToLocalTime().ToString("u")));
        command.Parameters.Add(new SQLiteParameter("@valor3", user_model.update_at.ToLocalTime().ToString("u")));
        command.Parameters.Add(new SQLiteParameter("@valor4", (int)user_model.privilege));
        command.Parameters.Add(new SQLiteParameter("@valor5", user_model.inserted_by));
        command.Parameters.Add(new SQLiteParameter("@valor6", user_model.phone_number));
        command.ExecuteNonQuery();
      }
    }
  }
  public List<UsersModel> RecuperarUsuario(Expression<Func<UsersModel, bool>>? expression = null) 
  {
    var usuarios = new List<UsersModel>();
    using (var connection = new SQLiteConnection(connectionString))
    {
      connection.Open();
      using(var command = connection.CreateCommand())
      {
        command.CommandText = "SELECT rowid, identifier, create_at, update_at, privilege, inserted_by, phone_number FROM usuarios";
        using(var dataReader = command.ExecuteReader())
        {
          if(!dataReader.HasRows) return usuarios;
          while(dataReader.Read())
          {
            var usuario = new UsersModel();
            usuario.rowid = Convert.ToInt64(dataReader["rowid"]);
            usuario.identifier = Convert.ToInt64(dataReader["identifier"]);
            usuario.create_at = (DateTime)dataReader["create_at"];
            usuario.update_at = (DateTime)dataReader["update_at"];
            usuario.privilege = (UsersModel.userLevel)dataReader["privilege"];
            usuario.inserted_by = Convert.ToInt64(dataReader["inserted_by"]);
            usuario.phone_number = Convert.ToInt64(dataReader["phone_number"]);
            usuarios.Add(usuario);
          }
          return (expression == null) ? usuarios : usuarios.AsQueryable().Where(expression).ToList();
        }
      }
    }
  }
  public UsersModel? RecuperarUsuario(Int64 identifier)
  {
    return RecuperarUsuario(u => u.identifier == identifier).SingleOrDefault();
  }
  public void AlterarUsuario(UsersModel user_model)
  {
    using(var connection = new SQLiteConnection(connectionString))
    {
      connection.Open();
      using(var command = connection.CreateCommand())
      {
        command.CommandText = "UPDATE usuarios SET " + 
          "phone_number = @valor1, privilege = @valor2, update_at = @valor3, " +
          "inserted_by = @valor4 WHERE rowid = @valor5";
        command.Parameters.Add(new SQLiteParameter("@valor1", user_model.phone_number));
        command.Parameters.Add(new SQLiteParameter("@valor2", (int)user_model.privilege));
        command.Parameters.Add(new SQLiteParameter("@valor3", user_model.update_at.ToString("u")));
        command.Parameters.Add(new SQLiteParameter("@valor4", user_model.inserted_by));
        command.Parameters.Add(new SQLiteParameter("@valor5", user_model.rowid));
        command.ExecuteNonQuery();
      }
    }
  }

  public void InserirSolicitacao(logsModel request)
  {
    using(var connection = new SQLiteConnection(connectionString))
    {
      connection.Open();
      using(var command = connection.CreateCommand())
      {
        command.CommandText = "INSERT INTO solicitacoes " +
        "(identifier, application, information, received_at) " +
        "VALUES (@valor1, @valor2, @valor3, @valor4)";
        command.Parameters.Add(new SQLiteParameter("@valor1", request.identifier));
        command.Parameters.Add(new SQLiteParameter("@valor2", request.application));
        command.Parameters.Add(new SQLiteParameter("@valor3", request.information));
        command.Parameters.Add(new SQLiteParameter("@valor4", request.received_at.ToLocalTime().ToString("u")));
        command.ExecuteNonQuery();
      }
    }
  }
  public List<logsModel> RecuperarSolicitacao(Expression<Func<logsModel, bool>>? expression = null)
  {
    var solicitacoes = new List<logsModel>();
    using (var connection = new SQLiteConnection(connectionString))
    {
      connection.Open();
      using(var command = connection.CreateCommand())
      {
        command.CommandText = "SELECT rowid, identifier, application, information, received_at, response_at, instance, status FROM usuarios";
        using(var dataReader = command.ExecuteReader())
        {
          if(!dataReader.HasRows) return solicitacoes;
          while(dataReader.Read())
          {
            var solicitacao = new logsModel();
            solicitacao.rowid = Convert.ToInt64(dataReader["rowid"]);
            solicitacao.identifier = Convert.ToInt64(dataReader["id"]);
            solicitacao.application = (String)dataReader["application"];
            solicitacao.information = Convert.ToInt64(dataReader["informacao"]);
            solicitacao.received_at = (DateTime)dataReader["received_at"];
            solicitacao.response_at = (DateTime)dataReader["response_at"];
            solicitacao.instance = (Int32)dataReader["instance"];
            solicitacao.status = (Int32)dataReader["status"];
            solicitacoes.Add(solicitacao);
          }
          return (expression == null) ? solicitacoes : solicitacoes.AsQueryable().Where(expression).ToList();
        }
      }
    }
  }
  public logsModel? RecuperarSolicitacao(Int64 identifier)
  {
    return RecuperarSolicitacao(s => s.rowid == identifier).SingleOrDefault();
  }
  public void AlterarSolicitacao(logsModel request)
  {
    using(var connection = new SQLiteConnection(connectionString))
    {
      connection.Open();
      using(var command = connection.CreateCommand())
      {
        command.CommandText = "UPDATE solicitacoes SET " + 
          "response_at = @valor1, instance = @valor2, status = @valor3 " +
          "WHERE rowid = @valor4";
        command.Parameters.Add(new SQLiteParameter("@valor1", request.response_at.ToLocalTime().ToString("u")));
        command.Parameters.Add(new SQLiteParameter("@valor2", request.instance));
        command.Parameters.Add(new SQLiteParameter("@valor3", request.status));
        command.Parameters.Add(new SQLiteParameter("@valor4", request.rowid));
        command.ExecuteNonQuery();
      }
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