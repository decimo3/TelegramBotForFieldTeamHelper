using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using telbot.handle;
using telbot.models;
using telbot.Helpers;
using telbot.Services;
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
    var config = Configuration.GetInstance(System.Environment.GetCommandLineArgs());
    var argumentos = new String[] {"/NH", "/FI", "\"IMAGENAME eq telbot.exe\""};
    var result = Executor.Executar("tasklist", argumentos, true);
    if(result.Where(r => r.Contains("telbot.exe")).ToList().Count > 1)
    {
      var twice = "Já tem uma instância do chatbot rodando!";
      ConsoleWrapper.Error(Entidade.Manager, new Exception(twice));
      ConsoleWrapper.Write(Entidade.Manager, "Aperte qualquer tecla para sair.");
      Console.ReadKey();
      System.Environment.Exit(1);
    }
    if(System.IO.File.Exists($"{config.CURRENT_PATH}\\telbot.exe.old"))
      System.IO.File.Delete($"{config.CURRENT_PATH}\\telbot.exe.old");
    telbot.Helpers.Updater.Update(config);
    Database.GetInstance(config);
    if(System.IO.File.Exists($"{config.CURRENT_PATH}\\{config.SAP_LOCKFILE}"))
      System.IO.File.Delete($"{config.CURRENT_PATH}\\{config.SAP_LOCKFILE}");
    var bot = new TelegramBotClient(config.BOT_TOKEN);
    var msg = HandleMessage.GetInstance(bot);
    using (var cts = new CancellationTokenSource())
    {
      bot.StartReceiving(updateHandler: HandleUpdate, pollingErrorHandler: HandleError, cancellationToken: cts.Token);
      Console.WriteLine($"< {DateTime.Now} Manager: Start listening for updates. Press enter to stop.");
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
      // Remover essa condicional assim que os métodos forem implementados
      if(config.BOT_ASSINCRONO)
      {
        HandleAsynchronous.Cooker();
        HandleAsynchronous.Waiter();
      }
      Console.ReadLine();
      cts.Cancel();
    }
  }
  private static async Task HandleUpdate(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
  {
    var chatbot = HandleMessage.GetInstance();
    var msg2json = System.Text.Json.JsonSerializer.Serialize<Update>(update);
    if(Configuration.GetInstance().IS_DEVELOPMENT)
    {
      ConsoleWrapper.Debug(Entidade.Usuario, msg2json);
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
    switch (update.Message.Type)
    {
      case MessageType.Text:
      {
        if(update.Message.Text == null)
        {
          var erroMessage = "O formato da mensagem não é reconhecido!";
          await chatbot.ErrorReport(
            id: update.Message.From.Id,
            error: new Exception(erroMessage),
            request: null
          );
          return;
        }
        await HandleTypeMessage.ManuscriptsType(
          usuario: usuario,
          recebido_em: update.Message.Date,
          mensagem: update.Message.Text
        );
        break;
      }
      case MessageType.Contact:
      {
        if(update.Message.Contact == null)
        {
          var erroMessage = "O formato da mensagem não é reconhecido!";
          await chatbot.ErrorReport(
            id: update.Message.From.Id,
            error: new Exception(erroMessage),
            request: null
          );
          return;
        }
        await HandleTypeMessage.PhoneNumberType(
          usuario: usuario,
          telefone: Convert.ToInt64(update.Message.Contact.PhoneNumber.Replace("+", ""))
        );
        break;
      }
      case MessageType.Photo:
      {
        if(update.Message.Photo == null)
        {
          var erroMessage = "O formato do imagem não é reconhecido!";
          await chatbot.ErrorReport(
            id: update.Message.From.Id,
            error: new Exception(erroMessage),
            request: null
          );
          return;
        }
        await HandleTypeMessage.PhotographType(
          usuario: usuario,
          recebido_em: update.Message.Date,
          photograph: update.Message.Photo.First().FileId,
          caption: update.Message.Caption
        );
        break;
      }
      case MessageType.Document:
      {
        if(update.Message.Document == null)
        {
          var erroMessage = "O formato do documento não é reconhecido!";
          await chatbot.ErrorReport(
            id: update.Message.From.Id,
            error: new Exception(erroMessage),
            request: null
          );
          return;
        }
        await HandleTypeMessage.DocumentType(
          usuario: usuario,
          recebido_em: update.Message.Date,
          document: update.Message.Document.FileId,
          caption: update.Message.Caption
        );
        break;
      }
      case MessageType.Video:
      {
        if(update.Message.Video == null)
        {
          var erroMessage = "O formato da vídeo não é reconhecido!";
          await chatbot.ErrorReport(
            id: update.Message.From.Id,
            error: new Exception(erroMessage),
            request: null
          );
          return;
        }
        await HandleTypeMessage.VideoclipType(
          usuario: usuario,
          recebido_em: update.Message.Date,
          videoclip: update.Message.Video.FileId,
          caption: update.Message.Caption
        );
        break;
      }
      default:
      {
        var erroMessage = "O formato da mensagem não é reconhecido!";
        await chatbot.ErrorReport(
          id: update.Message.From.Id,
          error: new Exception(erroMessage),
          request: null
        );
      }
      break;
    }
  }
  #pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
  private static async Task HandleError(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken)
  {
    ConsoleWrapper.Error(Entidade.Manager, exception);
  }
  #pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
