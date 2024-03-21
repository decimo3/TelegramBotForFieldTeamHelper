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
      bot.StartReceiving(updateHandler: HandleUpdate, pollingErrorHandler: HandleError, cancellationToken: cts.Token);
      Console.WriteLine($"< {DateTime.Now} Manager: Start listening for updates. Press enter to stop.");
      var msg = new handle.HandleMessage(bot);
      HandleAnnouncement.Comunicado(msg, cfg);
      if(cfg.VENCIMENTOS > 0)
      {
        while(true)
        {
          if(DateTime.Now.DayOfWeek != DayOfWeek.Saturday && DateTime.Now.DayOfWeek != DayOfWeek.Sunday)
          {
            var hora_agora = DateTime.Now.Hour;
            if(hora_agora >= 8 && hora_agora <= 19)
              HandleAnnouncement.Vencimento(msg, cfg);
          }
          Thread.Sleep(cfg.VENCIMENTOS);
        }
      }
      else
      {
        Console.ReadLine();
        // Send cancellation request to stop the bot
        cts.Cancel();
      }
    }
  }
  async Task HandleUpdate(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
  {
    if(cfg.IS_DEVELOPMENT)
    {
      var json_options = new System.Text.Json.JsonSerializerOptions();
      json_options.WriteIndented = true;
      Console.WriteLine(System.Text.Json.JsonSerializer.Serialize<Update>(update, json_options));
    }
    var msg = new HandleMessage(bot);
    await Recovery.ErrorSendMessageRecovery(msg);
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
        while(true)
        {
          if(!System.IO.File.Exists(cfg.LOCKFILE)) break;
          else System.Threading.Thread.Sleep(1_000);
        }
        System.IO.File.Create(cfg.LOCKFILE).Close();
        await HandleMessage(msg, update.Message!);
        System.IO.File.Delete(cfg.LOCKFILE);
      }
    }
    else await msg.ErrorReport(id: cfg.ID_ADM_BOT, error: new InvalidOperationException(update.ToString()));
    return;
  }
  #pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
  async Task HandleError(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken)
  {
    Temporary.ConsoleWriteError($"< {DateTime.Now} chatbot: {exception.Message}");
    return;
  }
  #pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
  async Task HandleMessage(HandleMessage msg, Message message)
  {
    if (message.From is null) return;
    var user = Database.recuperarUsuario(message.From.Id);
    if(user is null)
    {
      user = new(message.From.Id);
      Database.inserirUsuario(user);
      await msg.sendTextMesssageWraper(message.From.Id, "Seja bem vindo ao sistema de atendimento automático do chatbot!");
      await msg.sendTextMesssageWraper(message.From.Id, "Eu não estou autorizado a te passar informações no momento");
      await msg.sendTextMesssageWraper(message.From.Id, $"Seu identificador do Telegram é `{message.From.Id}`.");
      await msg.sendTextMesssageWraper(message.From.Id, "Informe ao seu supervisor esse identificador para ter acesso");
      return;
    }
    var has_txt = message.Text != null && message.Text.Length > 50;
    var has_jpg = message.Photo != null ? message.Photo.First().FileId : null;
    var has_mp4 = message.Video != null ? message.Video.FileId : null;
    if(user.pode_transmitir() && (has_jpg != null || has_mp4 != null || has_txt))
    {
      if(has_txt) await HandleAnnouncement.Comunicado(msg, cfg, user.id, message.Text, null, null);
      if(has_jpg != null) await HandleAnnouncement.Comunicado(msg, cfg, user.id, message.Caption, has_jpg, null);
      if(has_mp4 != null) await HandleAnnouncement.Comunicado(msg, cfg, user.id, message.Caption, null, has_mp4);
      return;
    }
    if (message.Text is null) return;
    Console.WriteLine($"> {message.Date.ToLocalTime()} usuario: {message.From.Id} escreveu: {message.Text}");
    if(user.has_privilege == UsersModel.userLevel.desautorizar)
    {
      await msg.sendTextMesssageWraper(message.From.Id, "Você foi banido do sistema chatbot, e não poderá mais usar seus serviços");
      await msg.sendTextMesssageWraper(message.From.Id, "Para restaurar o seu acesso ao sistema, solicite ao seu supervisor o acesso ao BOT.");
      await msg.sendTextMesssageWraper(message.From.Id, $"Seu identificador do telegram é {message.From.Id}, esse número deverá ser informado ao seu supervisor.");
      return;
    }
    if(user.phone_number == 0)
    {
      await msg.RequestContact(message.From.Id);
      return;
    }
    if(user.has_privilege == UsersModel.userLevel.eletricista)
    {
    // verifica se o cadastro de eletricistas expirou
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
    }
    var request = Validador.isRequest(message.Text, message.Date.ToLocalTime(), message.MessageId);
    if (request is null)
    {
      await msg.sendTextMesssageWraper(user.id, "Verifique o formato da informação e tente novamente da forma correta!");
      await msg.sendTextMesssageWraper(user.id, "Se tiver em dúvida de como usar o bot, digite /ajuda.");
      return;
    }
    if(cfg.SAP_OFFLINE && request.tipo != TypeRequest.gestao && request.tipo != TypeRequest.comando)
    {
      var messagem = "O ChatBOT não está funcionando no momento devido ao sistema SAP estar fora do ar.\n\nO BOT não tem como funcionar sem o SAP.";
      await msg.sendTextMesssageWraper(message.From.Id, messagem);
      return;
    }
    // Gets the installation of the request and since every request will be made by the installation
    if(request.tipo != TypeRequest.gestao && request.tipo != TypeRequest.comando)
    {
      var knockout = DateTime.Now.AddMinutes(-5);
      if(System.DateTime.Compare(knockout, request.received_at) > 0)
      {
        await msg.ErrorReport(user.id, new Exception(), request, "Sua solicitação expirou! Solicite novamente!");
        return;
      }
      var resposta = telbot.Temporary.executar(cfg, "desperta", request.informacao!);
      if(resposta.Any())
      {
        if(!resposta[0].StartsWith("ERRO")) request.informacao = resposta[0];
        else 
        {
          await msg.ErrorReport(user.id, new Exception(), request, resposta[0]);
          return;
        }
      }
      else
      {
        await msg.ErrorReport(user.id, new IndexOutOfRangeException("Não foi recebida nenhuma resposta do SAP"), request);
        return;
      }
    }
    if(cfg.IS_DEVELOPMENT == false)
    {
      if(Database.verificarRelatorio(request, user.id))
      {
        await msg.sendTextMesssageWraper(user.id, "Essa solicitação já foi respondida! Verifique a resposta enviada e se necessário solicite esclarecimentos para a monitora.");
        return;
      }
    }
    // When we get a command, we react accordingly
    switch(request.tipo)
    {
      case TypeRequest.comando: 
        await Command.HandleCommand(msg, user, request, cfg);
      break;
      case TypeRequest.gestao:
        if(!user.pode_autorizar())
        {
          await msg.sendTextMesssageWraper(user.id, "Você não tem permissão para alterar usuários!");
          break;
        }
        await Manager.HandleManager(msg, cfg, user, request);
      break;
      case TypeRequest.anyInfo:
        await Information.SendMultiples(msg, cfg, user, request);
      break;
      case TypeRequest.txtInfo:
        await Information.SendManuscripts(msg, cfg, user, request);
      break;
      case TypeRequest.picInfo: 
        await Information.SendPicture(msg, cfg, user, request); 
      break;
      case TypeRequest.xlsInfo:
        if(!user.pode_relatorios())
        {
          await msg.sendTextMesssageWraper(user.id, "Você não tem permissão para gerar relatórios!");
          break;
        }
        await Information.SendWorksheet(msg, cfg, user, request);
      break;
      case TypeRequest.pdfInfo:
        if(!cfg.GERAR_FATURAS)
        {
          await msg.ErrorReport(user.id, new Exception(), request, "O sistema SAP não está gerando faturas!");
          break;
        }
        if(request.aplicacao == "passivo" && (DateTime.Today.DayOfWeek == DayOfWeek.Friday || DateTime.Today.DayOfWeek == DayOfWeek.Saturday))
        {
          await msg.ErrorReport(user.id, new Exception(), request, "Essa aplicação não deve ser usada na sexta e no sábado!");
          break;
        }
        await Information.SendDocument(msg, cfg, user, request);
        break;
    }
    return;
  }
}
