using System.Threading;
using dotenv.net;
namespace telbot;
class Startup
{
  public static async Task Main(string[] args)
  {
    Console.WriteLine("#############################################");
    Console.WriteLine("# BOT de atendimento automágico MestreRuan  #");
    Console.WriteLine("# Author: decimo3 (github.com/decimo3)      #");
    Console.WriteLine("# Repository: TelegramBotForFieldTeamHelper #");
    Console.WriteLine("#############################################");
    if(args.Contains("--em-desenvolvimento")) DotEnv.Load();
    if(System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development") DotEnv.Load();
    var config = new Configuration(args);
    if(!config.IS_DEVELOPMENT)
    {
      await Updater.CheckUpdates();
    }
    /*
    Database.configurarBanco(config);
    var wakeup = new WakeUpSAP(config);
    Task task = Task.Factory.StartNew(() => {
      wakeup.Start();
    });
    var program = new Program(config);
    */
  }
}
