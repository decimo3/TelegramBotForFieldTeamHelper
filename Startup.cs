using dotenv.net;
namespace telbot;
class Startup
{
  public static void Main(string[] args)
  {
    if(args.Contains("--em-desenvolvimento")) DotEnv.Load();
    if(System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development") DotEnv.Load();
    var config = new Configuration(args);
    Database.configurarBanco(config);
    var program = new Program(config);
  }
}
