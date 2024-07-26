using dotenv.net;
using telbot.Helpers;
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
    var result = Temporary.executar("tasklist", "/NH /FI \"IMAGENAME eq telbot.exe\"", true);
    if(result.Where(r => r.Contains("telbot.exe")).ToList().Count > 1)
    {
      var twice = "Já tem uma instância do chatbot rodando!";
      ConsoleWrapper.Error(Entidade.Manager, new Exception(twice));
      ConsoleWrapper.Write(Entidade.Manager, "Aperte qualquer tecla para sair.");
      Console.ReadKey();
      System.Environment.Exit(1);
    }
    var config = new Configuration(args);
    if(File.Exists($"{config.CURRENT_PATH}\\telbot.exe.old"))
      File.Delete($"{config.CURRENT_PATH}\\telbot.exe.old");
    telbot.Helpers.Updater.Update(config);
    Database.configurarBanco(config);
    if(File.Exists($"{config.CURRENT_PATH}\\{config.SAP_LOCKFILE}"))
      File.Delete($"{config.CURRENT_PATH}\\{config.SAP_LOCKFILE}");
    var program = new Program(config);
  }
}
