using System.Data.SQLite;
using System.Linq.Expressions;
using telbot.Interfaces;
using telbot.models;
namespace telbot.Services;
public class SQLite : IDatabase
{
  private bool _disposed = false; // To detect redundant calls
  private readonly String connectionString = "Data Source=database.db";
  public SQLite(Configuration cfg)
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
            phone_number INT DEFAULT 0,
            username TEXT DEFAULT ''
            )";
        command.ExecuteNonQuery();
        command.CommandText = @$"CREATE TABLE IF NOT EXISTS solicitacoes(
            identifier INT NOT NULL,
            application VARCHAR(16) NOT NULL,
            information INT NOT NULL,
            request_type INT NOT NULL,
            received_at DATETIME NOT NULL,
            response_at DATETIME DEFAULT NULL,
            status INT DEFAULT 0,
            instance INT DEFAULT 0
            )";
        command.ExecuteNonQuery();
        command.CommandText = @$"CREATE TABLE IF NOT EXISTS faturas(
            filename VARCHAR(64) NOT NULL,
            instalation INT NOT NULL,
            timestamp DATETIME NOT NULL,
            status INT DEFAULT 0
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
          "(identifier, create_at, update_at, privilege, inserted_by, phone_number, username)" +
          "VALUES (@valor1, @valor2, @valor3, @valor4, @valor5, @valor6, @valor7)";
        command.Parameters.Add(new SQLiteParameter("@valor1", user_model.identifier));
        command.Parameters.Add(new SQLiteParameter("@valor2", user_model.create_at.ToString("u")));
        command.Parameters.Add(new SQLiteParameter("@valor3", user_model.update_at.ToString("u")));
        command.Parameters.Add(new SQLiteParameter("@valor4", (int)user_model.privilege));
        command.Parameters.Add(new SQLiteParameter("@valor5", user_model.inserted_by));
        command.Parameters.Add(new SQLiteParameter("@valor6", user_model.phone_number));
        command.Parameters.Add(new SQLiteParameter("@valor7", user_model.username));
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
        command.CommandText = "SELECT rowid, identifier, create_at, update_at, privilege, inserted_by, phone_number, username FROM usuarios";
        using(var dataReader = command.ExecuteReader())
        {
          if(!dataReader.HasRows) return usuarios;
          while(dataReader.Read())
          {
            var usuario = new UsersModel();
            usuario.rowid = dataReader.GetInt64(0);
            usuario.identifier = dataReader.GetInt64(1);
            usuario.create_at = dataReader.GetDateTime(2);
            usuario.update_at = dataReader.GetDateTime(3);
            usuario.privilege = (UsersModel.userLevel)dataReader.GetInt32(4);
            usuario.inserted_by = dataReader.GetInt64(5);
            usuario.phone_number = dataReader.GetInt64(6);
            usuario.username = dataReader.GetString(7);
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
          "inserted_by = @valor4, username = @valor6 WHERE rowid = @valor5";
        command.Parameters.Add(new SQLiteParameter("@valor1", user_model.phone_number));
        command.Parameters.Add(new SQLiteParameter("@valor2", (int)user_model.privilege));
        command.Parameters.Add(new SQLiteParameter("@valor3", user_model.update_at.ToString("u")));
        command.Parameters.Add(new SQLiteParameter("@valor4", user_model.inserted_by));
        command.Parameters.Add(new SQLiteParameter("@valor5", user_model.rowid));
        command.Parameters.Add(new SQLiteParameter("@valor6", user_model.username));
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
        "(identifier, application, information, received_at, response_at, request_type) " +
        "VALUES (@valor1, @valor2, @valor3, @valor4, @valor5, @valor6)";
        command.Parameters.Add(new SQLiteParameter("@valor1", request.identifier));
        command.Parameters.Add(new SQLiteParameter("@valor2", request.application));
        command.Parameters.Add(new SQLiteParameter("@valor3", request.information));
        command.Parameters.Add(new SQLiteParameter("@valor4", request.received_at.ToString("u")));
        command.Parameters.Add(new SQLiteParameter("@valor5", DateTime.MinValue.ToString("u")));
        command.Parameters.Add(new SQLiteParameter("@valor6", request.typeRequest));
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
        command.CommandText = "SELECT rowid, identifier, application, information, received_at, response_at, instance, status, request_type FROM solicitacoes WHERE status = 0";
        using(var dataReader = command.ExecuteReader())
        {
          if(!dataReader.HasRows) return solicitacoes;
          while(dataReader.Read())
          {
            var solicitacao = new logsModel();
            solicitacao.rowid = dataReader.GetInt64(0);
            solicitacao.identifier = dataReader.GetInt64(1);
            solicitacao.application = dataReader.GetString(2);
            solicitacao.information = dataReader.GetInt64(3);
            solicitacao.received_at = dataReader.GetDateTime(4);
            solicitacao.response_at = dataReader.GetDateTime(5);
            solicitacao.instance = dataReader.GetInt32(6);
            solicitacao.status = dataReader.GetInt32(7);
            solicitacao.typeRequest = (TypeRequest)dataReader.GetInt32(8);
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
        command.Parameters.Add(new SQLiteParameter("@valor1", request.response_at.ToString("u")));
        command.Parameters.Add(new SQLiteParameter("@valor2", request.instance));
        command.Parameters.Add(new SQLiteParameter("@valor3", request.status));
        command.Parameters.Add(new SQLiteParameter("@valor4", request.rowid));
        command.ExecuteNonQuery();
      }
    }
  }

  public void InserirFatura(pdfsModel fatura)
  {
    using(var connection = new SQLiteConnection(connectionString))
    {
      connection.Open();
      using(var command = connection.CreateCommand())
      {
        command.CommandText = "INSERT INTO faturas " +
        "(filename, instalation, timestamp, status) " +
        "VALUES (@valor1, @valor2, @valor3, @valor4)";
        command.Parameters.Add(new SQLiteParameter("@valor1", fatura.filename));
        command.Parameters.Add(new SQLiteParameter("@valor2", fatura.instalation));
        command.Parameters.Add(new SQLiteParameter("@valor3", fatura.timestamp));
        command.Parameters.Add(new SQLiteParameter("@valor4", fatura.status));
        command.ExecuteNonQuery();
      }
    }
  }
  public pdfsModel? RecuperarFatura(string filename)
  {
    return RecuperarFatura(f => f.filename == filename).SingleOrDefault();
  }
  public List<pdfsModel> RecuperarFatura(Expression<Func<pdfsModel, bool>>? expression = null)
  {
    var faturas = new List<pdfsModel>();
    using (var connection = new SQLiteConnection(connectionString))
    {
      connection.Open();
      using(var command = connection.CreateCommand())
      {
        command.CommandText = "SELECT rowid, filename, instalation, timestamp, status FROM faturas";
        using(var dataReader = command.ExecuteReader())
        {
          if(!dataReader.HasRows) return faturas;
          while(dataReader.Read())
          {
            var fatura = new pdfsModel();
            fatura.rowid = dataReader.GetInt64(0);
            fatura.filename = dataReader.GetString(1);
            fatura.instalation = dataReader.GetInt64(2);
            fatura.timestamp = dataReader.GetDateTime(3);
            fatura.status = (pdfsModel.Status)dataReader.GetInt32(4);
            faturas.Add(fatura);
          }
          return (expression == null) ? faturas : faturas.AsQueryable().Where(expression).ToList();
        }
      }
    }
  }
  public void AlterarFatura(pdfsModel fatura)
  {
    using(var connection = new SQLiteConnection(connectionString))
    {
      connection.Open();
      using(var command = connection.CreateCommand())
      {
        command.CommandText = "UPDATE faturas SET " + 
          "status = @valor1 WHERE rowid = @valor2";
        command.Parameters.Add(new SQLiteParameter("@valor1", fatura.status));
        command.Parameters.Add(new SQLiteParameter("@valor2", fatura.rowid));
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
    }
  }
  ~SQLite()
  {
    // Finalizer (optional)
    Dispose(false);
  }
}