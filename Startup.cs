using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using telbot.handle;
using telbot.models;
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
    var config = new Configuration(args);
    if(System.IO.File.Exists($"{config.CURRENT_PATH}\\telbot.exe.old"))
      System.IO.File.Delete($"{config.CURRENT_PATH}\\telbot.exe.old");
    telbot.Helpers.Updater.Update(config);
    Database.configurarBanco(config);
    if(System.IO.File.Exists($"{config.CURRENT_PATH}\\{config.SAP_LOCKFILE}"))
      System.IO.File.Delete($"{config.CURRENT_PATH}\\{config.SAP_LOCKFILE}");
    var program = new Program(config);
    var bot = new TelegramBotClient(config.BOT_TOKEN);
    var msg = new HandleMessage(bot);
    var tel = new HandleTelegram(config, bot, msg);
    using (var cts = new CancellationTokenSource())
    {
      bot.StartReceiving(updateHandler: tel.HandleUpdate, pollingErrorHandler: tel.HandleError, cancellationToken: cts.Token);
      Console.WriteLine($"< {DateTime.Now} Manager: Start listening for updates. Press enter to stop.");
      if(config.IS_DEVELOPMENT == false) HandleAnnouncement.Comunicado(msg, config);
      if(config.SAP_VENCIMENTO) HandleAnnouncement.Vencimento(msg, config, "vencimento", 7);
      if(config.SAP_BANDEIRADA) HandleAnnouncement.Vencimento(msg, config, "bandeirada", 7);
      if(config.OFS_MONITORAMENTO) HandleAnnouncement.Monitorado(msg, config);
      if(config.OFS_FINALIZACAO) HandleAnnouncement.Finalizacao(msg, config);
      if(config.PRL_SUBSISTEMA) HandleAnnouncement.Faturamento(msg, config);
      if(config.BOT_ASSINCRONO)
      {
        HandleAsynchronous.Soiree(msg, config);
        HandleAsynchronous.Cooker(msg, config);
        HandleAsynchronous.Waiter(msg, config);
      }
      Recovery.ErrorSendMessageRecovery(msg);
      Console.ReadLine();
      cts.Cancel();
    }
  }
}
