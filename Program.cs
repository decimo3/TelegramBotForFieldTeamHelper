using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using telbot.models;
using telbot.Helpers;
namespace telbot.handle;
public class HandleTelegram
{
  private readonly TelegramBotClient bot;
  private readonly Configuration cfg;
  private readonly HandleMessage msg;
  public HandleTelegram(Configuration cfg, TelegramBotClient bot, HandleMessage msg)
  {
    this.bot = bot;
    this.cfg = cfg;
    this.msg = msg;
  }
  public async Task HandleUpdate(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
  {
    var msg = new HandleMessage(bot);
    var msg2json = System.Text.Json.JsonSerializer.Serialize<Update>(update);
    if(cfg.IS_DEVELOPMENT)
    {
      ConsoleWrapper.Debug(Entidade.Usuario, msg2json);
    }
    if (update.Type != UpdateType.Message || update.Message == null)
    {
      await msg.ErrorReport(id: cfg.ID_ADM_BOT, error: new InvalidOperationException(), SAPerrorMessage: msg2json);
      return;
    }
    if (update.Message.From == null)
    {
      await msg.ErrorReport(id: cfg.ID_ADM_BOT, error: new InvalidOperationException(), SAPerrorMessage: msg2json);
      return;
    }
    //##################################################//
    //       Verifica se o usuário está cadastrado      //
    //       e se tem permissão para usar o sistema     //
    //##################################################//
    var user = Database.recuperarUsuario(update.Message.From.Id);
    if(user is null)
    {
      user = new(update.Message.From.Id);
      Database.inserirUsuario(user);
      await msg.sendTextMesssageWraper(update.Message.From.Id, "Seja bem vindo ao sistema de atendimento automático do chatbot!");
      await msg.sendTextMesssageWraper(update.Message.From.Id, "Eu não estou autorizado a te passar informações no momento");
      await msg.sendTextMesssageWraper(update.Message.From.Id, $"Seu identificador do Telegram é `{update.Message.From.Id}`.");
      await msg.sendTextMesssageWraper(update.Message.From.Id, "Informe ao seu supervisor esse identificador para ter acesso");
      return;
    }
    if(user.has_privilege == UsersModel.userLevel.desautorizar)
    {
      await msg.sendTextMesssageWraper(update.Message.From.Id, "Eu não estou autorizado a te passar informações no momento");
      await msg.sendTextMesssageWraper(update.Message.From.Id, "Para restaurar o seu acesso ao sistema, solicite ao seu supervisor o acesso ao BOT.");
      await msg.sendTextMesssageWraper(update.Message.From.Id, $"Seu identificador do telegram é `{update.Message.From.Id}`, esse número deverá ser informado ao seu supervisor.");
      return;
    }
    //##################################################//
    //       Verifica se o prazo da autorização de      //
    //     eletricistas, supervisores e controladores   //
    //##################################################//
    if(!user.pode_promover())
    {
      var prazo_expiracao = (user.has_privilege == UsersModel.userLevel.supervisor) ? cfg.DIAS_EXPIRACAO * 3 : cfg.DIAS_EXPIRACAO;
      DateTime expiracao = user.update_at.AddDays(prazo_expiracao);
      if(System.DateTime.Compare(DateTime.Now, expiracao) > 0)
      {
        await msg.sendTextMesssageWraper(update.Message.From.Id, "Sua autorização expirou e não posso mais te passar informações");
        await msg.sendTextMesssageWraper(update.Message.From.Id, "Solicite a autorização novamente para o seu supervisor!");
        await msg.sendTextMesssageWraper(update.Message.From.Id, $"Seu identificador do Telegram é {update.Message.From.Id}.");
        return;
      }
      DateTime sinalizar = user.update_at.AddDays(prazo_expiracao - 7);
      // Verifica se o cadastro está perto de expirar (7 dias antes) e avisa
      if(System.DateTime.Compare(DateTime.Now, sinalizar) >= 0)
      {
        await msg.sendTextMesssageWraper(update.Message.From.Id, "Sua autorização está quase expirando!");
        await msg.sendTextMesssageWraper(update.Message.From.Id, "Solicite a **atualização** para o seu supervisor!");
        await msg.sendTextMesssageWraper(update.Message.From.Id, $"Seu identificador do Telegram é {update.Message.From.Id}.");
      }
    }
    //##################################################//
    //       Verifica se o usuário possui telefone      //
    //     cadastrado, se não, ele solicita o cadastro  //
    //##################################################//
    if (update.Message.Type == MessageType.Contact)
    {
      if(update.Message.Contact == null)
      {
        await msg.ErrorReport(id: cfg.ID_ADM_BOT, error: new InvalidOperationException(), SAPerrorMessage: msg2json);
        return;
      }
      Database.inserirTelefone(update.Message.From!.Id, Int64.Parse(update.Message.Contact.PhoneNumber));
      await msg.RemoveRequest(update.Message.From.Id, update.Message.Contact.PhoneNumber);
      return;
    }
    if(user.phone_number == 0)
    {
      await msg.RequestContact(update.Message.From.Id);
      return;
    }
    //##################################################//
    //     Caso o tipo da mensagem seja localização     //
    //     Ele verificará os equipamentos próximos      //
    //##################################################//
    if (update.Message.Type == MessageType.Location)
    {
      if(update.Message.Location == null)
      {
        await msg.ErrorReport(id: cfg.ID_ADM_BOT, error: new InvalidOperationException(msg2json));
        return;
      }
      await Information.GetZoneInfo(msg, update.Message.From.Id, update.Message.Location.Latitude, update.Message.Location.Longitude, update.Message.Date);
      return;
    }
    //##################################################//
    //     Caso o tipo da mensagem possua alguma mídia  //
    //     Ele verificará permissão para envio          //
    //         de comunicados e os enviará              //
    //##################################################//
    if (update.Message.Type == MessageType.Photo || update.Message.Type == MessageType.Video || update.Message.Type == MessageType.Document)
    {
      if(!user.pode_transmitir())
      {
        await msg.sendTextMesssageWraper(user.id, "Você não possui permissão para enviar comunicados!");
        return;
      }
      var media_id = String.Empty;
      var mensagem = $"{update.Message.Caption}\n\n*ENVIADO POR: {update.Message.From.FirstName} {update.Message.From.LastName}*";
      var usuarios = Database.recuperarUsuario(u =>
      (
        u.has_privilege == UsersModel.userLevel.proprietario ||
        u.has_privilege == UsersModel.userLevel.administrador ||
        u.has_privilege == UsersModel.userLevel.comunicador ||
        (u.has_privilege == UsersModel.userLevel.eletricista && u.update_at.AddDays(cfg.DIAS_EXPIRACAO) > DateTime.Now) ||
        (u.has_privilege == UsersModel.userLevel.controlador && u.update_at.AddDays(cfg.DIAS_EXPIRACAO) > DateTime.Now) ||
        (u.has_privilege == UsersModel.userLevel.supervisor && u.update_at.AddDays(cfg.DIAS_EXPIRACAO * 3) > DateTime.Now)
      ));
      var has_jpg = update.Message.Photo != null ? update.Message.Photo.First().FileId : null;
      var has_mp4 = update.Message.Video != null ? update.Message.Video.FileId : null;
      var has_doc = update.Message.Document != null ? update.Message.Document.FileId : null;
      await HandleAnnouncement.Comunicado(usuarios, msg, cfg, user.id, mensagem, has_jpg, has_mp4, has_doc);
      await msg.sendTextMesssageWraper(user.id, "Comunicado enviado com sucesso!");
      return;
    }
    //##################################################//
    //     Caso o tipo da mensagem não seja os citados  //
    //     acima, ela deverá ser do tipo TEXT.          //
    //##################################################//
    if(update.Message.Type != MessageType.Text)
    {
      await msg.sendTextMesssageWraper(user.id, "O tipo de mensagem enviada não é suportado!");
      await msg.ErrorReport(id: cfg.ID_ADM_BOT, error: new InvalidOperationException(msg2json));
      return;
    }
    if(string.IsNullOrEmpty(update.Message.Text))
    {
      await msg.sendTextMesssageWraper(user.id, "O tipo de mensagem enviada não é suportado!");
      await msg.ErrorReport(id: cfg.ID_ADM_BOT, error: new InvalidOperationException(msg2json));
      return;
    }
    //##################################################//
    //     Caso o tipo da mensagem tenha mais           //
    //     de 50 caracteres, ela será tratada           //
    //         como comunicado de texto                 //
    //##################################################//
    if(update.Message.Text.Length > 50)
    {
      if(!user.pode_transmitir())
      {
        await msg.sendTextMesssageWraper(user.id, "Você não possui permissão para enviar comunicados!");
        return;
      }
      var mensagem = $"{update.Message.Text}\n\n*ENVIADO POR: {update.Message.From.FirstName} {update.Message.From.LastName}*";
      var usuarios = Database.recuperarUsuario(u =>
      (
        u.has_privilege == UsersModel.userLevel.proprietario ||
        u.has_privilege == UsersModel.userLevel.administrador ||
        u.has_privilege == UsersModel.userLevel.comunicador ||
        (u.has_privilege == UsersModel.userLevel.eletricista && u.update_at.AddDays(cfg.DIAS_EXPIRACAO) > DateTime.Now) ||
        (u.has_privilege == UsersModel.userLevel.controlador && u.update_at.AddDays(cfg.DIAS_EXPIRACAO) > DateTime.Now) ||
        (u.has_privilege == UsersModel.userLevel.supervisor && u.update_at.AddDays(cfg.DIAS_EXPIRACAO * 3) > DateTime.Now)
      ));
      await HandleAnnouncement.Comunicado(usuarios, msg, cfg, user.id, mensagem, null, null, null);
      await msg.sendTextMesssageWraper(user.id, "Comunicado enviado com sucesso!");
      return;
    }
    //##################################################//
    //     Tratando solicitações daqui em diante        //
    //##################################################//
    Console.WriteLine($"> {update.Message.Date.ToLocalTime()} usuario: {update.Message.From.Id} escreveu: {update.Message.Text}");
    var request = Validador.isRequest(update.Message.Text, update.Message.Date.ToLocalTime(), update.Message.MessageId);
    if (request is null)
    {
      await msg.sendTextMesssageWraper(user.id, "Verifique o formato da informação e tente novamente da forma correta!");
      await msg.sendTextMesssageWraper(user.id, "Se tiver em dúvida de como usar o bot, digite /ajuda.");
      return;
    }
    if(cfg.SAP_OFFLINE)
    {
      if(request.tipo != TypeRequest.gestao && request.tipo != TypeRequest.comando && request.tipo != TypeRequest.ofsInfo)
      {
        var messagem = "O ChatBOT não está funcionando no momento devido ao sistema SAP estar fora do ar.\n\nO BOT não tem como funcionar sem o SAP.";
        await msg.sendTextMesssageWraper(update.Message.From.Id, messagem);
        return;
      }
    }
    //##################################################//
    //     Verificando se o sistema SAP está ocupado    //
    //##################################################//
    while(true)
    {
      if(!System.IO.File.Exists(cfg.SAP_LOCKFILE)) break;
      else System.Threading.Thread.Sleep(1_000);
    }
    System.IO.File.Create(cfg.SAP_LOCKFILE).Close();
    //##################################################//
    //     Verificando se o sistema SAP está ocupado    //
    //##################################################//
    if(cfg.IS_DEVELOPMENT == false)
    {
      if(request.tipo != TypeRequest.gestao && request.tipo != TypeRequest.comando && request.tipo != TypeRequest.xlsInfo && request.tipo != TypeRequest.ofsInfo)
      {
        var knockout = DateTime.Now.AddMinutes(-5);
        if(System.DateTime.Compare(knockout, request.received_at) > 0)
        {
          await msg.ErrorReport(user.id, new Exception(), request, "Sua solicitação expirou! Solicite novamente!");
          System.IO.File.Delete(cfg.SAP_LOCKFILE);
          return;
        }
        var resposta = telbot.Temporary.executar(cfg, "instalacao", request.informacao!);
        if(resposta == null)
        {
          await msg.ErrorReport(user.id, new IndexOutOfRangeException("Não foi recebida nenhuma resposta do SAP"), request);
          System.IO.File.Delete(cfg.SAP_LOCKFILE);
          return;
        }
        if(!resposta.Any())
        {
          await msg.ErrorReport(user.id, new IndexOutOfRangeException("Não foi recebida nenhuma resposta do SAP"), request);
          System.IO.File.Delete(cfg.SAP_LOCKFILE);
          return;
        }
        if(resposta.First().StartsWith("ERRO"))
        {
          await msg.ErrorReport(user.id, new Exception(), request, String.Join('\n', resposta));
          System.IO.File.Delete(cfg.SAP_LOCKFILE);
          return;
        }
        request.informacao = Int64.Parse(resposta.First());
        if(Database.verificarRelatorio(request, user.id))
        {
          await msg.sendTextMesssageWraper(user.id, "Essa solicitação já foi respondida! Verifique a resposta enviada e se necessário solicite esclarecimentos para a monitora.");
          System.IO.File.Delete(cfg.SAP_LOCKFILE);
          return;
        }
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
      case TypeRequest.xyzInfo:
        await Information.SendCoordinates(msg, cfg, user, request);
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
        if(cfg.PRL_SUBSISTEMA)
        {
          await Information.SendMultiples(msg, cfg, user, request);
        }
        else
        {
          await Information.SendDocument(msg, cfg, user, request);
        }
        break;
      case TypeRequest.ofsInfo:
        if(!user.pode_relatorios())
        {
          await msg.sendTextMesssageWraper(user.id, "Você não tem permissão para receber evidências!");
          break;
        }
        await Information.SendMultiples(msg, cfg, user, request);
      break;
    }
    System.IO.File.Delete(cfg.SAP_LOCKFILE);
    return;
  }
  #pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
  public async Task HandleError(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken)
  {
    ConsoleWrapper.Error(Entidade.Manager, exception);
    if(System.IO.File.Exists(cfg.SAP_LOCKFILE)) System.IO.File.Delete(cfg.SAP_LOCKFILE);
    return;
  }
  #pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
