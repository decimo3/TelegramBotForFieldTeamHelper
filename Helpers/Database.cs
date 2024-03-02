using System.Data.SQLite;
namespace telbot;
public static class Database
{
  private static string connectionString = "Data Source=database.db";
  public static void configurarBanco(Configuration cfg)
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
        command.CommandText = @$"CREATE TABLE IF NOT EXISTS usersModel(
            id INT PRIMARY KEY,
            create_at DATETIME NOT NULL,
            update_at DATETIME NOT NULL,
            has_privilege BOOLEAN NOT NULL DEFAULT FALSE,
            inserted_by INT NOT NULL,
            phone_number INT DEFAULT 0
            )";
        command.ExecuteNonQuery();
        command.CommandText = @$"CREATE TABLE IF NOT EXISTS logsModel(
            id INT NOT NULL,
            aplicacao VARCHAR(16) NOT NULL,
            informacao INT NOT NULL,
            create_at DATETIME NOT NULL,
            received_at DATETIME NOT NULL,
            is_sucess BOOLEAN NOT NULL DEFAULT TRUE
            )";
        command.ExecuteNonQuery();
        if(recuperarUsuario(cfg.ID_ADM_BOT) is null)
        {
          command.CommandText = @$"INSERT INTO usersModel(id, create_at, update_at, has_privilege, inserted_by, phone_number)
          VALUES ({cfg.ID_ADM_BOT}, '{DateTime.Now.ToString("u")}', '{DateTime.Now.ToString("u")}', 1, {cfg.ID_ADM_BOT}, 0);";
          command.ExecuteNonQuery();
        }
        command.CommandText = @$"CREATE TABLE IF NOT EXISTS errorReport(
            identificador INT NOT NULL,
            mensagem TEXT,
            binario BLOB
            )";
        command.ExecuteNonQuery();
      }
      connection.Close();
    }
  }
  public static UsersModel? recuperarUsuario(long id)
  {
    using(var connection = new SQLiteConnection(connectionString))
    {
      connection.Open();
      using(var command = connection.CreateCommand())
      {
        command.CommandText = @$"SELECT id, create_at, update_at, has_privilege, inserted_by, phone_number FROM usersModel WHERE id = {id}";
        using(var dataReader = command.ExecuteReader())
        {
          if(!dataReader.HasRows) return null;
          dataReader.Read();
          return new UsersModel() {
            id = dataReader.GetInt64(0),
            create_at = dataReader.GetDateTime(1),
            update_at = dataReader.GetDateTime(2),
            has_privilege = dataReader.GetBoolean(3),
            inserted_by = dataReader.GetInt64(4),
            phone_number = dataReader.GetInt64(5)
          };
        }
      }
    }
  }
  public static void inserirUsuario(UsersModel user_model)
  {
    using(var connection = new SQLiteConnection(connectionString))
    {
      connection.Open();
      using(var command = connection.CreateCommand())
      {
        command.CommandText = @$"INSERT INTO usersModel(id, create_at, update_at, has_privilege, inserted_by)
        VALUES ({user_model.id}, '{user_model.create_at.ToString("u")}', '{user_model.update_at.ToString("u")}', {user_model.has_privilege}, {user_model.inserted_by})";
        command.ExecuteNonQuery();
      }
    }
  }
  public static void inserirRelatorio(logsModel log)
  {
    using(var connection = new SQLiteConnection(connectionString))
    {
      connection.Open();
      using(var command = connection.CreateCommand())
      {
        command.CommandText = @$"INSERT INTO logsModel(id, aplicacao, informacao, create_at, is_sucess, received_at)
        VALUES ({log.id}, '{log.solicitacao}', '{log.informacao}', '{log.create_at.ToString("u")}', {log.is_sucess}, '{log.received_at.ToString("u")}')";
        command.ExecuteNonQuery();
      }
    }
  }
  public static bool promoverUsuario(long id, long inserted_by)
  {
    try
    {
      recuperarUsuario(id);
      using(var connection = new SQLiteConnection(connectionString))
      {
        connection.Open();
        using(var command = connection.CreateCommand())
        {
          command.CommandText = @$"UPDATE usersModel SET has_privilege = 1, inserted_by = {inserted_by}, update_at = '{DateTime.Now.ToString("u")}' WHERE id = {id}";
          command.ExecuteNonQuery();
        }
      }
      return true;
    }
    catch
    {
      return false;
    }
  }
  public static bool inserirTelefone(long id, long phone)
  {
    try
    {
      if(recuperarUsuario(id) is null) return false;
      using(var connection = new SQLiteConnection(connectionString))
      {
        connection.Open();
        using(var command = connection.CreateCommand())
        {
          command.CommandText = @$"UPDATE usersModel SET phone_number = {phone}, update_at = '{DateTime.Now.ToString("u")}' WHERE id = {id}";
          command.ExecuteNonQuery();
        }
      }
      return true;
    }
    catch
    {
      return false;
    }
  }
  public static bool atualizarUsuario(long id, long inserted_by)
  {
    try
    {
      if(recuperarUsuario(id) is null) return false;
      using(var connection = new SQLiteConnection(connectionString))
      {
        connection.Open();
        using(var command = connection.CreateCommand())
        {
          command.CommandText = @$"UPDATE usersModel SET inserted_by = {inserted_by}, update_at = '{DateTime.Now.ToString("u")}' WHERE id = {id}";
          command.ExecuteNonQuery();
        }
      }
      return true;
    }
    catch
    {
      return false;
    }
  }
  public static List<logsModel> statusTelbot()
  {
    var logs = new List<logsModel>();
    var dia = new DateTime(year: 2023, month: 4, day: 22).ToString("yyyy-MM-dd");
    try
    {
      using (var connection = new SQLiteConnection(connectionString))
      {
        connection.Open();
        using(var command = connection.CreateCommand())
        {
          command.CommandText = $"SELECT id, aplicacao, informacao, create_at, is_sucess, received_at FROM logsModel WHERE date(create_at) == date('{dia}')";
          using(var dataReader = command.ExecuteReader())
          {
            if(!dataReader.HasRows) throw new InvalidOperationException("Aconteceu algum erro no banco!");
            while(dataReader.Read())
            {
              logs.Add(new logsModel() {
                id = dataReader.GetInt64(0),
                solicitacao = dataReader.GetString(1),
                informacao = dataReader.GetString(2),
                create_at = dataReader.GetDateTime(3),
                is_sucess = dataReader.GetBoolean(4),
                received_at = dataReader.GetDateTime(5)
              });
            }
            return logs;
          }
        }
      }
    }
    catch
    {
      return logs;
    }
  }
  public static List<long> todosUsuarios()
  {
    var todes = new List<long>();
    using (var connection = new SQLiteConnection(connectionString))
    {
      connection.Open();
      using(var command = connection.CreateCommand())
      {
        command.CommandText = $"SELECT id FROM usersModel";
        using(var dataReader = command.ExecuteReader())
        {
          if(!dataReader.HasRows) throw new InvalidOperationException("Aconteceu algum erro no banco!");
          while(dataReader.Read())
          {
            todes.Append(dataReader.GetInt64(0));
          }
          return todes;
        }
      }
    }
  }
  public static bool verificarRelatorio(models.Request request, long id)
  {
    using var connection = new SQLiteConnection(connectionString);
    connection.Open();
    using var command = connection.CreateCommand();
    command.CommandText = @$"SELECT COUNT(*) FROM logsmodel WHERE aplicacao = '{request.aplicacao}' AND informacao = '{request.informacao}' AND id = '{id}' AND DATE(create_at) = '{DateTime.Now.ToString("yyyy-MM-dd")}' AND is_sucess = 1;";
    using var dataReader = command.ExecuteReader();
    if (!dataReader.HasRows) return false;
    else dataReader.Read();
    return dataReader.GetInt32(0) > 0;
  }
  public static void InserirPerdido(long id, string mensagem)
  {
    using(var connection = new SQLiteConnection(connectionString))
    {
      connection.Open();
      using(var command = connection.CreateCommand())
      {
        command.CommandText = @$"INSERT INTO errorReport (identificador, mensagem) VALUES ('{id}', '{mensagem}');";
        command.ExecuteNonQuery();
      }
    }
  }
  public static void InserirPerdido(errorReport report)
  {
    byte[] bytes = Array.Empty<byte>();
    if(report.binario != null) 
    {
      var memoryStream = new MemoryStream();
      report.binario.CopyTo(memoryStream);
      bytes = memoryStream.ToArray();
    }
    using (var connection = new SQLiteConnection(connectionString))
    {
      connection.Open();
      using (var command = connection.CreateCommand())
      {
        command.CommandText = $"INSERT INTO errorReport (identificador, mensagem, binario) VALUES (@id, @message, @data);";
        command.Parameters.AddWithValue("@id", report.identificador);
        command.Parameters.AddWithValue("@message", report.mensagem);
        command.Parameters.AddWithValue("@data", bytes);
        command.ExecuteNonQuery();
      }
    }
  }
  public static List<errorReport> RecuperarPerdido()
  {
    var errorReportList = new List<errorReport>();
    using (var connection = new SQLiteConnection(connectionString))
    {
      connection.Open();
      using (var command = connection.CreateCommand())
      {
        command.CommandText = $"SELECT identificador, mensagem, binario FROM errorReport;";
        using (var reader = command.ExecuteReader())
        {
          if(!reader.HasRows) return errorReportList;
          while(reader.Read())
          {
            var report = new errorReport();
            report.identificador = reader.GetInt64(0);
            report.mensagem = reader.GetString(1);
            report.binario = (reader["binario"] is DBNull) ? Stream.Null : reader.GetStream(2);
            errorReportList.Add(report);
          }
        }
      }
    }
    return errorReportList;
  }
  public static void ExcluirPerdidos()
  {
    using (var connection = new SQLiteConnection(connectionString))
    {
      connection.Open();
      using (var command = connection.CreateCommand())
      {
        command.CommandText = $"DELETE FROM errorReport;";
        command.ExecuteNonQuery();
      }
    }
  }
}
