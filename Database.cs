using System.Data.SQLite;
namespace telbot;
public static class Database
{
  private static string connectionString = "Data Source=database.db";
  private static long ID_ADM_BOT = Int64.Parse(System.Environment.GetEnvironmentVariable("ID_ADM_BOT")!);
  public static void configurarBanco()
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
            inserted_by INT NOT NULL
            )";
        command.ExecuteNonQuery();
        command.CommandText = @$"CREATE TABLE IF NOT EXISTS logsModel(
            id INT NOT NULL,
            aplicacao VARCHAR(16) NOT NULL,
            informacao INT NOT NULL,
            create_at DATETIME NOT NULL,
            is_sucess BOOLEAN NOT NULL DEFAULT TRUE
            )";
        command.ExecuteNonQuery();
        try
        {
          recuperarUsuario(ID_ADM_BOT);
        }
        catch
        {
          command.CommandText = @$"INSERT INTO usersModel(id, create_at, update_at, has_privilege, inserted_by)
          VALUES ({ID_ADM_BOT}, '{DateTime.Now.ToString("u")}', '{DateTime.Now.ToString("u")}', 1, {ID_ADM_BOT});";
          command.ExecuteNonQuery();
        }
      }
      connection.Close();
    }
  }
  public static UsersModel recuperarUsuario(long id)
  {
    using(var connection = new SQLiteConnection(connectionString))
    {
      connection.Open();
      using(var command = connection.CreateCommand())
      {
        command.CommandText = @$"SELECT id, create_at, update_at, has_privilege, inserted_by FROM usersModel WHERE id = {id}";
        using(var dataReader = command.ExecuteReader())
        {
          if(!dataReader.HasRows) throw new InvalidOperationException("O usuário não existe no banco de dados!");
          dataReader.Read();
          return new UsersModel() {
            id = dataReader.GetInt64(0),
            create_at = dataReader.GetDateTime(1),
            update_at = dataReader.GetDateTime(2),
            has_privilege = dataReader.GetBoolean(3),
            inserted_by = dataReader.GetInt64(4)
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
        command.CommandText = @$"INSERT INTO logsModel(id, aplicacao, informacao, create_at, is_sucess) VALUES ({log.id}, '{log.solicitacao}', '{log.informacao}', '{log.create_at.ToString("u")}', {log.is_sucess})";
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
  public static bool atualizarUsuario(long id, long inserted_by)
  {
    try
    {
      recuperarUsuario(id);
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
  public static string statusTelbot()
  {
    var dia = new DateTime(year: 2023, month: 4, day: 22).ToString("yyyy-MM-dd");
    Console.WriteLine(dia);
    int[] valores = {};
    var comandos = new List<string>();
    comandos.Add($"SELECT COUNT(*) FROM logsModel WHERE date(create_at) = date('{dia}')");
    comandos.Add($"SELECT COUNT(*) FROM logsModel WHERE date(create_at) = date('{dia}') AND aplicacao = 'telefone' OR aplicacao = 'contato'");
    comandos.Add($"SELECT COUNT(*) FROM logsModel WHERE date(create_at) = date('{dia}') AND aplicacao = 'leiturista' OR aplicacao = 'roteiro'");
    comandos.Add($"SELECT COUNT(*) FROM logsModel WHERE date(create_at) = date('{dia}') AND aplicacao = 'fatura' OR aplicacao = 'debito'");
    comandos.Add($"SELECT COUNT(*) FROM logsModel WHERE date(create_at) = date('{dia}') AND is_sucess = 0");
    try
    {
      using (var connection = new SQLiteConnection(connectionString))
      {
        connection.Open();
        for (int i = 0; i < comandos.Count(); i++)
        {
          using(var command = connection.CreateCommand())
          {
            command.CommandText = comandos[i];
            using(var dataReader = command.ExecuteReader())
            {
              if(!dataReader.HasRows) throw new InvalidOperationException("Aconteceu algum erro no banco!");
              dataReader.Read();
              valores.Append(dataReader.GetInt16(0));
            }
          }
        }
      }
      return $"Telefone: {valores[1]}\nLeiturista: {valores[2]}\nFaturas: {valores[3]}\nOutros: {valores[0] - valores.Sum()}\nTotal: {valores[0]}\nSucessos:{valores[4]/valores[0]}";
    }
    catch
    {
      return "Aconteceu algum erro no banco!";
    }
  }
}
