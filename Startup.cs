using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using telbot.handle;
using telbot.models;
using telbot.Helpers;
using telbot.Services;
using Microsoft.Extensions.Logging;
namespace telbot;
class Startup
{
  static async Task Main(String[] args)
  {
    var logger = Logger.GetInstance<Startup>();
    logger.LogInformation("#############################################");
    logger.LogInformation("# BOT de atendimento automágico MestreRuan  #");
    logger.LogInformation("# Author: decimo3 (github.com/decimo3)      #");
    logger.LogInformation("# Repository: TelegramBotForFieldTeamHelper #");
    logger.LogInformation("#############################################");
    var config = Configuration.GetInstance(System.Environment.GetCommandLineArgs());
    if(HandleAnnouncement.Executador("bot.exe") > 1)
    {
      logger.LogError("Já tem uma instância do chatbot rodando!");
      logger.LogInformation("Aperte qualquer tecla para sair.");
      Console.ReadKey();
      System.Environment.Exit(1);
    }
    var oldExecutableFile = System.IO.Path.Combine(
      System.AppContext.BaseDirectory, "bot.exe.old");
    if(System.IO.File.Exists(oldExecutableFile))
      System.IO.File.Delete(oldExecutableFile);
    telbot.Helpers.Updater.Update();
    Database.GetInstance(config);
    var bot = new TelegramBotClient(config.BOT_TOKEN);
    var msg = HandleMessage.GetInstance(bot);
    using (var cts = new CancellationTokenSource())
    {
      bot.StartReceiving(updateHandler: HandleUpdate, pollingErrorHandler: HandleError, cancellationToken: cts.Token);
      logger.LogInformation("Start listening for updates. Press enter to stop.");
      if(config.IS_DEVELOPMENT == false) HandleAnnouncement.Comunicado();
      if(config.SAP_VENCIMENTO) HandleAnnouncement.Vencimento("vencimento", 7);
      if(config.SAP_BANDEIRADA) HandleAnnouncement.Vencimento("bandeirada", 7);
      if(config.OFS_MONITORAMENTO)
      {
        var filhos = new String[] {"ofs.exe", "chrome.exe", "chromedriver.exe"};
        HandleAnnouncement.Executador("ofs.exe", new String[] {"slower"}, filhos);
      }
      if(config.PRL_SUBSISTEMA)
      {
        var filhos = new String[] {"prl.exe", "chrome.exe", "chromedriver.exe"};
        HandleAnnouncement.Executador("prl.exe", new String[] {"slower"}, filhos);
      }
      HandleAnnouncement.Executador("cscript.exe", new String[] {"erroDialog.vbs"}, null);
      var sap_instance_check_args = new String[] { "instancia", "5", "0"};
      HandleAnnouncement.Executador("sap.exe", sap_instance_check_args, null);
      # pragma warning disable CS4014
      HandleAsynchronous.Chief
      (
        minInstance: 0,
        maxInstance: config.SAP_INSTANCIA,
        s =>
          s.typeRequest != TypeRequest.gestao &&
          s.typeRequest != TypeRequest.comando &&
          s.status == 0
      );
      # pragma warning restore CS4014
      var pdf_handler = new PdfHandle();
      pdf_handler.Watch();
      pdf_handler.Sender();
      Console.ReadLine();
      cts.Cancel();
    }
  }
  private static async Task HandleUpdate(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
  {
    var logger = Logger.GetInstance<Startup>();
    var chatbot = HandleMessage.GetInstance();
    var msg2json = System.Text.Json.JsonSerializer.Serialize<Update>(update);
    if(Configuration.GetInstance().IS_DEVELOPMENT)
    {
      logger.LogDebug(msg2json);
    }
    //##################################################//
    //       Ignora updates que não sejam mensagens     //
    //##################################################//
    if (update.Type != UpdateType.Message || update.Message == null || update.Message.From == null) return;
    //##################################################//
    //    Verifica se o usuário possui autorização      //
    //##################################################//
    var usuario = await Manager.HandleSecury(
      identificador: update.Message.From.Id,
      recebido_em: update.Message.Date
    );
    if(usuario == null) return;
    //##################################################//
    //       Verifica se o usuário possui telefone      //
    //##################################################//
    if(usuario.phone_number == 0 && update.Message.Type != MessageType.Contact)
    {
      await chatbot.RequestContact(update.Message.From.Id);
      return;
    }
    //##################################################//
    // direciona para um método correspondente ao tipo  //
    //##################################################//
    var datahora = update.Message.Date > DateTime.Now ? update.Message.Date.ToLocalTime() : update.Message.Date;
    switch (update.Message.Type)
    {
      case MessageType.Text:
      {
        await HandleTypeMessage.ManuscriptsType(
          usuario: usuario,
          recebido_em: datahora,
          mensagem: update.Message.Text!
        );
        break;
      }
      case MessageType.Contact:
      {
        await HandleTypeMessage.PhoneNumberType(
          usuario: usuario,
          telefone: Convert.ToInt64(update.Message.Contact!.PhoneNumber.Replace("+", "")),
          username: update.Message.From.FirstName + " " + update.Message.From.LastName
        );
        break;
      }
      case MessageType.Photo:
      {
        await HandleTypeMessage.PhotographType(
          usuario: usuario,
          recebido_em: datahora,
          photograph: update.Message.Photo!.First().FileId,
          caption: update.Message.Caption
        );
        break;
      }
      case MessageType.Document:
      {
        await HandleTypeMessage.DocumentType(
          usuario: usuario,
          recebido_em: datahora,
          document: update.Message.Document!.FileId,
          caption: update.Message.Caption
        );
        break;
      }
      case MessageType.Video:
      {
        await HandleTypeMessage.VideoclipType(
          usuario: usuario,
          recebido_em: datahora,
          videoclip: update.Message.Video!.FileId,
          caption: update.Message.Caption
        );
        break;
      }
      case MessageType.Location:
      {
        await HandleTypeMessage.CoordinatesType(
          usuario: usuario,
          recebido_em: datahora,
          lat: update.Message.Location!.Latitude,
          lon: update.Message.Location!.Longitude
        );
        break;
      }
      default:
      {
        var erroMessage = "O formato da mensagem não é reconhecido!";
        await chatbot.ErrorReport(
          error: new Exception(erroMessage),
          request: new logsModel() {
              identifier = update.Message.From.Id,
              application = "nullmessage",
              received_at = datahora,
              response_at = DateTime.Now,
              typeRequest = TypeRequest.nullInfo,
              status = 400
          });
      }
      break;
    }
  }
  #pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
  private static async Task HandleError(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken)
  {
    Logger.GetInstance<Startup>().LogError(
      exception, "Erro crítico que poderia parar a aplicação: "
    );
  }
  #pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
