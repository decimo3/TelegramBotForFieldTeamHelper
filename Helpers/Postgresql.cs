using telbot.Interfaces;
namespace telbot.Helpers
{
  public sealed class PostgreSQL : IDatabase
  {
    private static readonly PostgreSQL instance = new PostgreSQL();
    private Npgsql.NpgsqlConnection connection;
    static PostgreSQL() {}
    private PostgreSQL()
    {
      if(System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        dotenv.net.DotEnv.Fluent().WithEnvFiles(".env").Load();
      var dbhost = System.Environment.GetEnvironmentVariable("PGHOST");
      if(dbhost is null) throw new InvalidOperationException("Environment variable PGHOST is not set!");
      var dbport = System.Environment.GetEnvironmentVariable("PGPORT");
      if(dbport is null) throw new InvalidOperationException("Environment variable PGPORT is not set!");
      var dbuser = System.Environment.GetEnvironmentVariable("PGUSER");
      if(dbuser is null) throw new InvalidOperationException("Environment variable PGUSER is not set!");
      var dbpass = System.Environment.GetEnvironmentVariable("PGPASSWORD");
      if(dbpass is null) throw new InvalidOperationException("Environment variable PGPASSWORD is not set!");
      var dbbase = "chatbot";
      var stringconnection = new Npgsql.NpgsqlConnectionStringBuilder()
      {
        Host = dbhost,
        Port = Int32.Parse(dbport),
        Username = dbuser,
        Password = dbpass,
        Database = dbbase
      };
      connection = new Npgsql.NpgsqlConnection(stringconnection.ConnectionString);
      connection.Open();
    }
    public static PostgreSQL Instance { get { return instance; } }
    public Npgsql.NpgsqlConnection GetConnection() { return connection; }
    public void CloseConnection()
    {
      if(connection != null && connection.State == System.Data.ConnectionState.Open)
      {
        connection.Close();
      }
    }
    public void inserirUsuario(UsersModel user, Int64 inserted_by) 
    {
      var sql = @"INSERT INTO usersModel (id, create_at, update_at, has_privilege, inserted_by, phone_number) VALUES ($1), ($2), ($3), ($4), ($5), ($6)";
      using (var cmd = new Npgsql.NpgsqlCommand(sql, connection))
      {
        cmd.Parameters = {};
        cmd.ExecuteNonQuery();
      }
    }
    public void alterarUsuario(UsersModel user_model, Int64 inserted_by) {}
    public UsersModel recuperarUsuario(long id) {}
    public List<UsersModel> recuperarUsuario() {}
    public List<UsersModel> recuperarUsuario(Expression<Func<UsersModel, bool>> expression) {}
    public void inserirRelatorio(logsModel logs) {}
    public logsModel recuperarRelatorio(String aplicacao, Int64 informacao) {}
    public List<logsModel> recuperarRelatorio() {}
    public List<logsModel> recuperarRelatorio(Expression<Func<logsModel, bool>> expression) {}
    public void InserirPerdido(errorReport report) {}
    public List<errorReport> recuperarPerdido() {}
    public void excluirPerdidos() {}
  }
}