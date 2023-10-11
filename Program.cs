using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using telbot.handle;
using telbot.models;

namespace telbot;
public class Program
{
  private TelegramBotClient bot;
  private Configuration cfg;
  public Program(Configuration cfg)
  {
    this.cfg = cfg;
    // instantiates a new telegram bot api client with the specified token
    bot = new TelegramBotClient(cfg.BOT_TOKEN);
    // 
    using (var cts = new CancellationTokenSource())
    {
      // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool, so we use cancellation token
      bot.StartReceiving(updateHandler: HandleUpdate, pollingErrorHandler: HandleError, cancellationToken: cts.Token);
      // Tell the user the bot is online
      Console.WriteLine($"< {DateTime.Now} Manager: Start listening for updates. Press enter to stop.");
      Console.ReadLine();
      // Send cancellation request to stop the bot
      cts.Cancel();
    }
  }
  async Task HandleUpdate(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
  {
    var msg = new HandleMessage(bot);
    if (update.Type == UpdateType.Message)
    {
      if (update.Message!.Type == MessageType.Contact && update.Message.Contact != null)
      {
        Database.inserirTelefone(update.Message.From!.Id, Int64.Parse(update.Message.Contact.PhoneNumber));
        await msg.RemoveRequest(update.Message.From.Id, update.Message.Contact.PhoneNumber);
        return;
      }
      else
      {
        await HandleMessage(update.Message!);
      }
    }
    
    else await msg.ErrorReport(id: cfg.ID_ADM_BOT, error: new InvalidOperationException(update.ToString()));
    return;
  }
  async Task HandleError(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken)
  {
    await Console.Error.WriteLineAsync(exception.Message);
    await Console.Error.WriteLineAsync(exception.StackTrace);
  }
  async Task HandleMessage(Message message)
  {
    var msg = new HandleMessage(bot);
    if (message.From is null) return;
    if (message.Text is null) return;
    Console.WriteLine($"> {message.Date.ToLocalTime()} usuario: {message.From.Id} escreveu: {message.Text}");
    var user = Database.recuperarUsuario(message.From.Id);
    if(user is null)
    {
      await msg.sendTextMesssageWraper(message.From.Id, "Eu não estou autorizado a te passar informações!");
      await msg.sendTextMesssageWraper(message.From.Id, $"Seu identificador do Telegram é {message.From.Id}.");
      await msg.sendTextMesssageWraper(message.From.Id, "Informe ao seu supervisor esse identificador para ter acesso ao BOT");
      return;
    }
    if(user.phone_number == 0)
    {
      await msg.RequestContact(message.From.Id);
      return;
    }
    if(cfg.SAP_OFFLINE)
    {
      var messagem = "O ChatBOT não está funcionando no momento devido ao sistema SAP estar fora do ar.\n\nO BOT não tem como funcionar sem o SAP.";
      await msg.sendTextMesssageWraper(message.From.Id, messagem);
      return;
    }
    // verifica se o cadastro expirou
    DateTime expiracao = user.update_at.AddDays(cfg.DIAS_EXPIRACAO);
    DateTime sinalizar = user.update_at.AddDays(cfg.DIAS_EXPIRACAO - 7);
    if(System.DateTime.Compare(DateTime.Now, expiracao) > 0)
    {
      await msg.sendTextMesssageWraper(message.From.Id, "Sua autorização expirou e não posso mais te passar informações");
      await msg.sendTextMesssageWraper(message.From.Id, "Solicite a autorização novamente para o seu supervisor!");
      await msg.sendTextMesssageWraper(message.From.Id, $"Seu identificador do Telegram é {message.From.Id}.");
      return;
    }
    // Verifica se o cadastro está perto de expirar (7 dias antes) e avisa
    if(System.DateTime.Compare(DateTime.Now, sinalizar) >= 0)
    {
      await msg.sendTextMesssageWraper(message.From.Id, "Sua autorização está quase expirando!");
      await msg.sendTextMesssageWraper(message.From.Id, "Solicite a **atualização** para o seu supervisor!");
      await msg.sendTextMesssageWraper(message.From.Id, $"Seu identificador do Telegram é {message.From.Id}.");
    }
    var request = Validador.isRequest(message.Text, message.Date.ToLocalTime());
    if (request is null)
    {
      await msg.sendTextMesssageWraper(user.id, "Verifique o formato da informação e tente novamente da forma correta!");
      await msg.sendTextMesssageWraper(user.id, "Se tiver em dúvida de como usar o bot, digite /ajuda.");
      return;
    }
    if(Database.verificarRelatorio(request))
    {
      await msg.sendTextMesssageWraper(user.id, "Essa solicitação já foi respondida! Verifique a resposta enviada e se necessário solicite esclarecimentos para a monitora.");
      return;
    }
    // When we get a command, we react accordingly
    if (request.tipo == TypeRequest.comando)
    {
      await new HandleCommand(msg, user, request, cfg).routerCommand();
      return;
    }
    if(request.tipo == TypeRequest.gestao)
    {
      await new HandleManager(msg, cfg, user, request).routerManager();
      return;
    }
    await new HandleInformation(msg, cfg, user, request).routeInformation();
    return;
  }
}
