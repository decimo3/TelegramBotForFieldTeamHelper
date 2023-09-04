using dotenv.net;
namespace telbot;
class Startup
{
  public static void Main(string[] args)
  {
    Console.WriteLine("#############################################");
    Console.WriteLine("# BOT de atendimento automágico MestreRuan  #");
    Console.WriteLine("# Author: decimo3 (github.com/decimo3)      #");
    Console.WriteLine("# Repository: TelegramBotForFieldTeamHelper #");
    Console.WriteLine("#############################################");
    var agora = DateTime.Now;
    var prazo = new DateTime(year: 2023, month: 10, day: 1);
    if(agora > prazo)
    {
      Console.BackgroundColor = ConsoleColor.Red;
      Console.Beep();
      Console.Write("O período de licença de uso expirou!");
      Console.BackgroundColor = ConsoleColor.Black;
      return;
    }
    if(args.Contains("--em-desenvolvimento")) DotEnv.Load();
    if(System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development") DotEnv.Load();
    var config = new Configuration(args);
    if(config.HOSTNAME != config.ALLOWED_PC)
    {
      Console.BackgroundColor = ConsoleColor.Red;
      Console.Beep();
      Console.Write("A licença de uso não permite o uso em outra máquina!");
      Console.BackgroundColor = ConsoleColor.Black;
      return;
    }
    Database.configurarBanco(config);
    var program = new Program(config);
  }
}
