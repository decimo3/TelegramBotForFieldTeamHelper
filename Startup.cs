using dotenv.net;
namespace telbot;
class Startup
{
  public static void Main(string[] args)
  {
    if(args.Contains("--in-development")) DotEnv.Load();
    Database.configurarBanco();
    var program = new Program(args);
  }
}
