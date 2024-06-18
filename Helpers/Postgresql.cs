using telbot.Interfaces;
namespace telbot.Helpers
{
  public sealed class PostgreSQL
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
      var dbbase = System.Environment.GetEnvironmentVariable("PGDATABASE");
      if(dbbase is null) throw new InvalidOperationException("Environment variable PGDATABASE is not set!");
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
  }
}