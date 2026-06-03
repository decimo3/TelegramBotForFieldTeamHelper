using Npgsql;
using System.Linq.Expressions;
using telbot.Interfaces;
using telbot.models;
namespace telbot.Services;
public class PostgreSQL : IDatabase
{
  private bool _disposed = false; // To detect redundant calls
  private Npgsql.NpgsqlDataSource connection;
  public PostgreSQL(Configuration cfg)
  {
    var connectionBuilder = new Npgsql.NpgsqlConnectionStringBuilder();
    connectionBuilder.Host = System.Environment.GetEnvironmentVariable("POSTGRES_HOST") ??
      throw new ArgumentNullException("A variável POSTGRES_HOST não está definida!");
    var postgres_port = System.Environment.GetEnvironmentVariable("POSTGRES_PORT") ??
      throw new ArgumentNullException("A variável POSTGRES_PORT não está definida!");
    if(Int32.TryParse(postgres_port, out Int32 result)) connectionBuilder.Port = result;
    connectionBuilder.Username = System.Environment.GetEnvironmentVariable("POSTGRES_USER") ??
      throw new ArgumentNullException("A variável POSTGRES_USER não está definida!");
    connectionBuilder.Password = System.Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ??
      throw new ArgumentNullException("A variável POSTGRES_PASSWORD não está definida!");
    connectionBuilder.Database = "chatbot";
    this.connection = Npgsql.NpgsqlDataSource.Create(connectionBuilder);
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
  public void InserirUsuario(UsersModel usuario)
  {
    using(var command = connection.CreateCommand())
    {
      command.CommandText = "INSERT INTO usuarios" + 
        "(identifier, create_at, update_at, privilege, inserted_by, phone_number, username)" +
        "VALUES (@valor1, @valor2, @valor3, @valor4, @valor5, @valor6, @valor7)";
      command.Parameters.Add(new NpgsqlParameter("valor1", usuario.identifier));
      command.Parameters.Add(new NpgsqlParameter("valor2", usuario.create_at));
      command.Parameters.Add(new NpgsqlParameter("valor3", usuario.update_at));
      command.Parameters.Add(new NpgsqlParameter("valor4", (int)usuario.privilege));
      command.Parameters.Add(new NpgsqlParameter("valor5", usuario.inserted_by));
      command.Parameters.Add(new NpgsqlParameter("valor6", usuario.phone_number));
      command.Parameters.Add(new NpgsqlParameter("valor7", usuario.username));
      command.ExecuteNonQuery();
    }
  }
  public UsersModel? RecuperarUsuario(long identificador)
  {
    return RecuperarUsuario(u => u.identifier == identificador).SingleOrDefault();
  }
  public List<UsersModel> RecuperarUsuario(Expression<Func<UsersModel, bool>>? expression = null)
  {
    var usuarios = new List<UsersModel>();
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
      }
    }
    return (expression == null) ? usuarios : usuarios.AsQueryable().Where(expression).ToList();
  }
  public void AlterarUsuario(UsersModel usuario)
  {
    using(var command = connection.CreateCommand())
    {
      command.CommandText = "UPDATE usuarios SET " + 
        "phone_number = @valor1, privilege = @valor2, update_at = @valor3, " +
        "inserted_by = @valor4, username = @valor6 WHERE rowid = @valor5";
      command.Parameters.Add(new NpgsqlParameter("valor1", usuario.phone_number));
      command.Parameters.Add(new NpgsqlParameter("valor2", (int)usuario.privilege));
      command.Parameters.Add(new NpgsqlParameter("valor3", usuario.update_at));
      command.Parameters.Add(new NpgsqlParameter("valor4", usuario.inserted_by));
      command.Parameters.Add(new NpgsqlParameter("valor5", usuario.rowid));
      command.Parameters.Add(new NpgsqlParameter("valor6", usuario.username));
      command.ExecuteNonQuery();
    }
  }
  public void InserirSolicitacao(logsModel request)
  {
    using(var command = connection.CreateCommand())
    {
      command.CommandText = "INSERT INTO solicitacoes " +
      "(identifier, application, information, received_at, response_at, request_type, message) " +
      "VALUES (@valor1, @valor2, @valor3, @valor4, @valor5, @valor6, @valor7)";
      command.Parameters.Add(new NpgsqlParameter("valor1", request.identifier));
      command.Parameters.Add(new NpgsqlParameter("valor2", request.application));
      command.Parameters.Add(new NpgsqlParameter("valor3", request.information));
      command.Parameters.Add(new NpgsqlParameter("valor4", request.received_at));
      command.Parameters.Add(new NpgsqlParameter("valor5", DateTime.MinValue));
      command.Parameters.Add(new NpgsqlParameter("valor6", (int)request.typeRequest));
      command.Parameters.Add(new NpgsqlParameter("valor7", request.message));
      command.ExecuteNonQuery();
    }
  }
  public logsModel? RecuperarSolicitacao(long rowid)
  {
    return RecuperarSolicitacao(s => s.rowid == rowid).SingleOrDefault();
  }
  public List<logsModel> RecuperarSolicitacao(Expression<Func<logsModel, bool>>? expression = null)
  {
    var solicitacoes = new List<logsModel>();
    using(var command = connection.CreateCommand())
    {
      command.CommandText = "SELECT rowid, identifier, application, information, received_at, response_at, instance, status, request_type, message FROM solicitacoes";
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
          solicitacao.message = dataReader.IsDBNull(9) ? String.Empty : dataReader.GetString(9);
          solicitacoes.Add(solicitacao);
        }
      }
    }
    return (expression == null) ? solicitacoes : solicitacoes.AsQueryable().Where(expression).ToList();
  }
  public void AlterarSolicitacao(logsModel request)
  {
    using(var command = connection.CreateCommand())
    {
      command.CommandText = "UPDATE solicitacoes SET " + 
        "response_at = @valor1, instance = @valor2, status = @valor3, " +
        "information = @valor5 WHERE rowid = @valor4";
      command.Parameters.Add(new NpgsqlParameter("valor1", request.response_at));
      command.Parameters.Add(new NpgsqlParameter("valor2", request.instance));
      command.Parameters.Add(new NpgsqlParameter("valor3", request.status));
      command.Parameters.Add(new NpgsqlParameter("valor4", request.rowid));
      command.Parameters.Add(new NpgsqlParameter("valor5", request.information));
      command.ExecuteNonQuery();
    }
  }
  public void InserirFatura(pdfsModel fatura)
  {
    using(var command = connection.CreateCommand())
    {
      command.CommandText = "INSERT INTO faturas " +
      "(filename, instalation, timestamp, status) " +
      "VALUES (@valor1, @valor2, @valor3, @valor4)";
      command.Parameters.Add(new NpgsqlParameter("@valor1", fatura.filename));
      command.Parameters.Add(new NpgsqlParameter("@valor2", fatura.instalation));
      command.Parameters.Add(new NpgsqlParameter("@valor3", fatura.timestamp));
      command.Parameters.Add(new NpgsqlParameter("@valor4", (int)fatura.status));
      command.ExecuteNonQuery();
    }
  }
  public pdfsModel? RecuperarFatura(string filename)
  {
    return RecuperarFatura(f => f.filename == filename).SingleOrDefault();
  }
  public List<pdfsModel> RecuperarFatura(Expression<Func<pdfsModel, bool>>? expression = null)
  {
    var faturas = new List<pdfsModel>();
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
      }
    }
    return (expression == null) ? faturas : faturas.AsQueryable().Where(expression).ToList();
  }
  public void AlterarFatura(pdfsModel fatura)
  {
    using(var command = connection.CreateCommand())
    {
      command.CommandText = "UPDATE faturas SET " + 
        "status = @valor1 WHERE rowid = @valor2";
      command.Parameters.Add(new NpgsqlParameter("@valor1", (int)fatura.status));
      command.Parameters.Add(new NpgsqlParameter("@valor2", fatura.rowid));
      command.ExecuteNonQuery();
    }
  }
  public void RemoverFatura(Int64 rowid)
  {
    using(var command = connection.CreateCommand())
    {
      command.CommandText = "DELETE FROM faturas WHERE rowid = @valor";
      command.Parameters.Add(new NpgsqlParameter("@valor", rowid));
      command.ExecuteNonQuery();
    }
  }
  public void RemoverFatura()
  {
    using(var command = connection.CreateCommand())
    {
      command.CommandText = "TRUNCATE faturas ONLY RESTART IDENTITY";
      command.ExecuteNonQuery();
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
        this.connection.Dispose();
      }
      // Dispose unmanaged resources here.
      _disposed = true;
    }
  }

  public void InserirDocumento(DocsModel documento)
  {
    using var command = connection.CreateCommand();
    command.CommandText = "INSERT INTO documentos " +
      "(filename, message_id, identifier, updated_at, parent) " +
      "VALUES (@valor1, @valor2, @valor3, @valor4, @valor5)";
    command.Parameters.Add(new NpgsqlParameter("@valor1", documento.filename));
    command.Parameters.Add(new NpgsqlParameter("@valor2", documento.messageId));
    command.Parameters.Add(new NpgsqlParameter("@valor3", documento.identifier));
    command.Parameters.Add(new NpgsqlParameter("@valor4", documento.updatedAt));
    command.Parameters.Add(new NpgsqlParameter("@valor5", documento.parent));
    command.ExecuteNonQuery();
  }

  public DocsModel? RecuperarDocumento(string identifier)
  {
    return RecuperarDocumento(f => f.identifier == identifier).SingleOrDefault();
  }

  public List<DocsModel> RecuperarDocumento(Expression<Func<DocsModel, bool>>? expression = null)
  {
    var documentos = new List<DocsModel>();
    using var command = connection.CreateCommand();
    command.CommandText = "SELECT rowid, filename, message_id, identifier, updated_at, is_outdated, parent FROM documentos";
    using var dataReader = command.ExecuteReader();
    if(!dataReader.HasRows) return documentos;
    while(dataReader.Read())
    {
      documentos.Add(new DocsModel
      {
        rowid = dataReader.GetInt64(0),
        filename = dataReader.GetString(1),
        messageId = dataReader.GetString(2),
        identifier = dataReader.GetString(3),
        updatedAt = dataReader.GetDateTime(4),
        IsOutdated = dataReader.GetBoolean(5),
        parent = dataReader.GetString(6),
      });
    }
    return (expression == null) ? documentos : documentos.AsQueryable().Where(expression).ToList();
  }

  public void AlterarDocumento(DocsModel documento)
  {
    using var command = connection.CreateCommand();
    command.CommandText = "UPDATE documentos SET " +
      "message_id = @valor1, updated_at = @valor2, is_outdated = @valor3 WHERE rowid = @valor4";
    command.Parameters.Add(new NpgsqlParameter("@valor1", documento.messageId));
    command.Parameters.Add(new NpgsqlParameter("@valor2", documento.updatedAt));
    command.Parameters.Add(new NpgsqlParameter("@valor3", documento.IsOutdated));
    command.Parameters.Add(new NpgsqlParameter("@valor4", documento.rowid));
    command.ExecuteNonQuery();
  }

  ~PostgreSQL()
  {
    // Finalizer (optional)
    Dispose(false);
  }
}