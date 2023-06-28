using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
namespace telbot;
public class Program
{
  // Identificador do administrador do BOT
  private long ID_ADM_BOT;
  private int DIAS_EXPIRACAO;
  private bool GERAR_FATURAS;
  private bool SAP_OFFLINE;
  // instantiates a new telegram bot api client with the specified token
  private TelegramBotClient bot;
  public Program(string[] args)
  {
    GERAR_FATURAS = args.Contains("--sem-faturas") ? false : true;
    SAP_OFFLINE = args.Contains("--sap-offline") ? true : false;
    // Identificador do administrador do BOT
    ID_ADM_BOT = Int64.Parse(System.Environment.GetEnvironmentVariable("ID_ADM_BOT")!);
    // Define quantos dias a equipe terá acesso ao sistema sem renovar a autorização
    DIAS_EXPIRACAO = 30;
    // instantiates a new telegram bot api client with the specified token
    bot = new TelegramBotClient(System.Environment.GetEnvironmentVariable("TOKEN")!);
    // 
    using (var cts = new CancellationTokenSource())
    {
      // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool, so we use cancellation token
      bot.StartReceiving(updateHandler: HandleUpdate, pollingErrorHandler: HandleError, cancellationToken: cts.Token);
      // Tell the user the bot is online
      Console.WriteLine("Start listening for updates. Press enter to stop");
      Console.ReadLine();
      // Send cancellation request to stop the bot
      cts.Cancel();
    }
  }
  // Each time a user interacts with the bot, this method is called
  async Task HandleUpdate(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
  {
    if (update.Type == UpdateType.Message) await HandleMessage(update.Message!);
    else await ErrorReport(ID_ADM_BOT, string.Empty, string.Empty, new InvalidOperationException(update.ToString()));
    return;
  }
  async Task HandleError(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken)
  {
    await ErrorReport(ID_ADM_BOT, string.Empty, string.Empty, exception);
    await Console.Error.WriteLineAsync(exception.Message);
    await Console.Error.WriteLineAsync(exception.StackTrace);
  }
  async Task HandleMessage(Message message)
  {
    if (message.From is null) return;
    if (message.Text is null) return;
    // Print message update to console
    Console.WriteLine($"> {message.Date.ToLocalTime()} usuario: {message.From.Id} escreveu: {message.Text}");
    UsersModel user;
    string text = message.Text;
    try
    {
      user = Database.recuperarUsuario(message.From.Id);
    }
    catch
    {
      await sendTextMesssageWraper(message.From.Id, "Eu não estou autorizado a te passar informações!");
      await sendTextMesssageWraper(message.From.Id, $"Seu identificador do Telegram é {message.From.Id}.");
      await sendTextMesssageWraper(message.From.Id, "Informe ao seu supervisor esse identificador para ter acesso ao BOT");
      return;
    }
    if(SAP_OFFLINE)
    {
      var messagem = "O ChatBOT não está funcionando no momento devido ao sistema SAP estar fora do ar.\n\nO BOT não tem como funcionar sem o SAP.";
      await sendTextMesssageWraper(message.From.Id, messagem);
      return;
    }
    // verifica se o cadastro expirou
    DateTime expiracao = user.update_at.AddDays(DIAS_EXPIRACAO);
    DateTime sinalizar = user.update_at.AddDays(DIAS_EXPIRACAO - 7);
    if(System.DateTime.Compare(DateTime.Now, expiracao) > 0)
    {
      await sendTextMesssageWraper(message.From.Id, "Sua autorização expirou e não posso mais te passar informações");
      await sendTextMesssageWraper(message.From.Id, "Solicite a autorização novamente para o seu supervisor!");
      await sendTextMesssageWraper(message.From.Id, $"Seu identificador do Telegram é {message.From.Id}.");
      return;
    }
    // Verifica se o cadastro está perto de expirar (7 dias antes) e avisa
    if(System.DateTime.Compare(DateTime.Now, sinalizar) >= 0)
    {
      await sendTextMesssageWraper(message.From.Id, "Sua autorização está quase expirando!");
      await sendTextMesssageWraper(message.From.Id, "Solicite a **atualização** para o seu supervisor!");
      await sendTextMesssageWraper(message.From.Id, $"Seu identificador do Telegram é {message.From.Id}.");
    }
    if (!(text.Length > 0))
    {
      await sendTextMesssageWraper(user.id, "Não estou programado para responder a solicitações que não sejam mensagens de texto!");
      await sendTextMesssageWraper(user.id, "Verifique o formato da informação e tente novamente da forma correta!");
      await sendTextMesssageWraper(user.id, "Se tiver em dúvida de como usar o bot, digite /ajuda.");
      return;
    }
    // When we get a command, we react accordingly
    if (text.StartsWith("/"))
    {
      await HandleCommand(user.id, text);
      return;
    }
    text = text.ToLower();
    string[] args = text.Split(" ");
    if (args.Count() != 2)
    {
      await sendTextMesssageWraper(user.id, "Verifique o formato da informação e tente novamente da forma correta!");
      await sendTextMesssageWraper(user.id, "Se tiver em dúvida de como usar o bot, digite /ajuda.");
      return;
    }
    try
    {
      // verifica se a ordem dos operandos está correta
      if(!Validador.orderOperandos(args[0], args[1]))
      {
        // trocando a ordem dos operandos para a ordem aceita pelo script
        var temp = args[0];
        args[0] = args[1];
        args[1] = temp;
      }
    }
    catch
    {
      await sendTextMesssageWraper(user.id, "Verifique o formato da informação e tente novamente da forma correta!");
      await sendTextMesssageWraper(user.id, "Se tiver em dúvida de como usar o bot, digite /ajuda.");
      return;
    }
    if ((args[0] == "autorizar") || (args[0] == "atualizar"))
    {
      if(!user.has_privilege)
      {
        await sendTextMesssageWraper(user.id, "Você não tem permissão para alterar usuários!");
        Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], false));
        return;
      }
      if(Int64.TryParse(args[1], out long id))
      {
        try
        {
          Database.recuperarUsuario(id);
          if(Database.atualizarUsuario(id, user.id))
          {
            await sendTextMesssageWraper(id, "Usuário atualizado com sucesso!");
            await sendTextMesssageWraper(user.id, "Usuário atualizado com sucesso!");
            Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], true));
          }
          else
          {
            await sendTextMesssageWraper(user.id, "Houve um problema em atualizar o usuário");
            await sendTextMesssageWraper(user.id, "Verifique as informações e tente novamente");
            Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], false));
          }
        }
        catch
        {
          try
          {
            Database.inserirUsuario(new UsersModel(id, user.id));
            await sendTextMesssageWraper(id, "Usuário autorizado com sucesso!");
            await sendTextMesssageWraper(user.id, "Usuário autorizado com sucesso!");
            Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], true));
          }
          catch
          {
            await sendTextMesssageWraper(user.id, "Houve um problema em autorizar o usuário");
            await sendTextMesssageWraper(user.id, "Verifique as informações e tente novamente");
            Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], false));
          }
          return;
        }
      }
      else
      {
        await sendTextMesssageWraper(user.id, "O identificador do usuário não é válido!");
        Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], false));
      }
      return;
    }
    if (args[0] == "promover")
    {
      if(user.id != ID_ADM_BOT)
      {
        await sendTextMesssageWraper(user.id, "Você não tem permissão para promover usuários!");
        Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], false));
        return;
      }
      if(Int64.TryParse(args[1], out long id))
      {
        if(Database.promoverUsuario(id, user.id))
        {
          await sendTextMesssageWraper(user.id, "Usuário promovido com sucesso!");
          Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], true));
        }
        else
        {
          await sendTextMesssageWraper(user.id, "Houve um problema em promover o usuário");
          await sendTextMesssageWraper(user.id, "Verifique as informações e tente novamente");
          Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], false));
        }
      }
      else
      {
        await sendTextMesssageWraper(user.id, "O identificador do usuário não é válido!");
        Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], false));
      }
      return;
    }
    if ((args[0] == "fatura" || args[0] == "debito" || args[0] == "passivo") && GERAR_FATURAS == false)
    {
      await sendTextMesssageWraper(user.id, "O sistema SAP não está gerando faturas no momento!");
      Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], false));
      return;
    }
    if (args[0] == "passivo" && (DateTime.Today.DayOfWeek == DayOfWeek.Friday || DateTime.Today.DayOfWeek == DayOfWeek.Saturday))
    {
      await sendTextMesssageWraper(user.id, "Essa aplicação não deve ser usada na sexta e no sábado!\n\nNotas de recorte devem ter todas as faturas cobradas!");
      Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], false));
      return;
    }
    if ((args[0] == "relatorio") || (args[0] == "manobra"))
    {
      await sendTextMesssageWraper(user.id, "A informação solicitada não foi implementada ainda!");
      Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], false));
      return;
    }
    var resposta = telbot.Temporary.executar(args[0], args[1]);
    if ((resposta.Count == 0) || (resposta is null))
    {
      await ErrorReport(user.id, args[0], args[1], new Exception("Erro no script do SAP"));
      return;
    }
    if(resposta[0].StartsWith("ERRO"))
    {
      await ErrorReport(user.id, args[0], args[1], new Exception("Erro no script do SAP"), resposta[0]);
      return;
    }
    if ((args[0] == "relatorio") || (args[0] == "manobra"))
    {
      try
      {
        //Utilitarios.CreateCSV(resposta);
        await using Stream stream = System.IO.File.OpenRead(@$"{Temporary.CURRENT_PATH}\tmp\relatorio.csv");
        await bot.SendDocumentAsync(user.id, document: new Telegram.Bot.Types.InputFiles.InputOnlineFile(content: stream, fileName: $"{message.Date.ToShortTimeString()}.csv"));
        stream.Dispose();
        Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], true));
      }
      catch (System.Exception error)
      {
        await ErrorReport(user.id, args[0], args[1], error);
      }
      return;
    }
    if (args[0] == "fatura" || args[0] == "debito" || args[0] == "passivo")
    {
      try
      {
        foreach (string fatura in resposta)
        {
          if (fatura == "None" || fatura == null || fatura == "")
          {
            continue;
          }
          await using Stream stream = System.IO.File.OpenRead(@$"{Temporary.CURRENT_PATH}\tmp\{fatura}");
          await bot.SendDocumentAsync(user.id, document: new Telegram.Bot.Types.InputFiles.InputOnlineFile(content: stream, fileName: fatura));
          stream.Dispose();
        }
        Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], true));
      }
      catch (System.Exception error)
      {
        await ErrorReport(user.id, args[0], args[1], error);
      }
      return;
    }
    if ((args[0] == "leiturista") || (args[0] == "roteiro") || (args[0] == "historico") || (args[0] == "pendente") || (args[0] == "histórico") || (args[0] == "agrupamento"))
    {
      try
      {
        telbot.Temporary.executar(resposta);
        await using Stream stream = System.IO.File.OpenRead(@$"{Temporary.CURRENT_PATH}\tmp\temporario.png");
        await bot.SendPhotoAsync(user.id, photo: new Telegram.Bot.Types.InputFiles.InputOnlineFile(content: stream));
        stream.Dispose();
        System.IO.File.Delete(@$"{Temporary.CURRENT_PATH}\tmp\temporario.png");
        Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], true));
        if((args[0] == "agrupamento") && (DateTime.Today.DayOfWeek == DayOfWeek.Friday))
        await sendTextMesssageWraper(user.id, "*ATENÇÃO:* Não pode cortar agrupamento por nota de recorte!");
        await sendTextMesssageWraper(user.id, $"Enviado relatorio de {args[0]}!", false);
      }
      catch (System.Exception error)
      {
        await ErrorReport(user.id, args[0], args[1], error);
      }
      return;
    }
    if (args[0] == "telefone" || args[0] == "coordenada" || args[0] == "localização" || args[0] == "contato" || (args[0] == "medidor"))
    {
      string textoMensagem = String.Empty;
      foreach (var res in resposta)
      {
        textoMensagem += res;
        textoMensagem += "\n";
      }
      await sendTextMesssageWraper(user.id, textoMensagem);
      Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], true));
      return;
    }
    await sendTextMesssageWraper(user.id, "Não entendi o comando, verifique se está correto!");
    Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], false));
    return;
  }
  async Task HandleCommand(long userId, string message)
  {
    var command = message.Split(" ")[0];
    var re1 = new Regex("\".*\"");
    var texto = re1.Match(message);
    var re2 = new Regex("[0-9]{1,12}");
    var destinatario = re2.Match(message);
    switch (command)
    {
      case "/start":
        await sendTextMesssageWraper(userId, "Seja bem vindo ao programa de automação de respostas do MestreRuan");
        await sendTextMesssageWraper(userId, "Se tiver em dúvida de como usar o bot, digite /ajuda.");
        break;
      case "/ajuda":
        await sendTextMesssageWraper(userId, "Digite o tipo de informação que deseja e depois o número da nota ou instalação.");
        await sendTextMesssageWraper(userId, "*TELEFONE* ou *CONTATO* para receber todos os telefones no cadastro do cliente;");
        await sendTextMesssageWraper(userId, "*COORDENADA* para receber um link da localização no cadastro do cliente;");
        await sendTextMesssageWraper(userId, "*LEITURISTA* ou *ROTEIRO* para receber a lista de instalações ordenada por horário;");
        await sendTextMesssageWraper(userId, "*PENDENTE* para receber a lista de débitos para aquela instalação do cliente;");
        await sendTextMesssageWraper(userId, "*FATURA* ou *DEBITO* _(sem acentuação)_ para receber as faturas vencidas em PDF (limite de 5 faturas)");
        await sendTextMesssageWraper(userId, "*HISTORICO* _(sem acentuação)_ para receber a lista com os 5 últimos serviços para a instalação;");
        await sendTextMesssageWraper(userId, "*MEDIDOR* para receber as informações referentes ao medidor;");
        await sendTextMesssageWraper(userId, "*AGRUPAMENTO* para receber as informações referentes ao PC;");
        await sendTextMesssageWraper(userId, "Todas as solicitações não possuem acentuação e são no sigular (não tem o 's' no final).");
        await sendTextMesssageWraper(userId, "Estou trabalhando para trazer mais funções em breve");
        break;
      case "/ping":
        await sendTextMesssageWraper(userId, "Estou de prontidão aguardando as solicitações! (^.^)");
        break;
      case "/dados":
        Temporary.extratoDiario();
        Stream stream = System.IO.File.OpenRead("dados.csv");
        await bot.SendDocumentAsync(userId, document: new Telegram.Bot.Types.InputFiles.InputOnlineFile(content: stream, fileName: $"{DateTime.Now.ToString("dd-MM-yyyy_HHmmss")}.csv"));
        stream.Dispose();
        System.IO.File.Delete("dados.csv");
        break;
      case "/status":
        var statusSap = Temporary.executar("conecao", "0");
        if(statusSap.Count == 0)
        {
          statusSap.Add("offline");
        }
        var logs = Database.statusTelbot();
        var todos = logs.Count;
        var leiturista = (from f in logs where (f.solicitacao == "leiturista" && f.solicitacao == "roteiro") select f).Count();
        var faturas = (from e in logs where (e.solicitacao == "fatura" && e.solicitacao == "debito") select e).Count();
        var telefone = (from c in logs where (c.solicitacao == "contato" && c.solicitacao == "telefone") select c).Count();
        await sendTextMesssageWraper(userId, $"Sistema SAP: {statusSap[0]}\n\nEstatísticas:\n");
        break;
      case "/enviar":
        if(!Int64.TryParse(destinatario.Value, out long id))
        {
          await sendTextMesssageWraper(userId, "O identificador do usuário não é válido!");
          break;
        }
        if(texto is null)
        {
          await sendTextMesssageWraper(userId, "O texto não foi encontrado na mensagem!");
        }
        else
        {
          await sendTextMesssageWraper(id, $"Mensagem do administrador: {texto.Value}");
          await sendTextMesssageWraper(userId, "Mensagem enviada com sucesso!");
        }
        break;
      case "/todos":
        if(texto.Value is null)
        {
          await sendTextMesssageWraper(userId, "O texto não foi encontrado na mensagem!");
        }
        else
        {
          var todes = Database.todosUsuarios();
          var msg = $"Mensagem do administrador para todos: {texto.Value}";
          foreach(var um in todes)
          {
            sendTextMesssageWraper(um, msg).Wait();
          }
          await sendTextMesssageWraper(userId, "Mensagem enviada com sucesso!");
        }
        break;
      case "/teste":
        await sendTextMesssageWraper(userId, "Teste realizado com sucesso");
        break;
      default:
        await sendTextMesssageWraper(userId, "Comando solicitado não foi programado! Verifique e tente um válido");
        break;
    }
    await Task.CompletedTask;
  }
  async Task ErrorReport(long userId, string aplicacao, string informacao, Exception error, string? SAPerrorMessage=null)
  {

    await sendTextMesssageWraper(ID_ADM_BOT, $"Aplicação: {aplicacao} Informação: {informacao}", false);
    if(SAPerrorMessage is not null) await sendTextMesssageWraper(userId, SAPerrorMessage);
    await sendTextMesssageWraper(userId, "Não foi possível processar a sua solicitação!");
    await sendTextMesssageWraper(userId, "Solicite a informação para o monitor(a)");
    Database.inserirRelatorio(new logsModel(userId, aplicacao, informacao, false));
    return;
  }
  async Task sendTextMesssageWraper(long userId, string message, bool enviar=true)
  {
    try
    {
      if(enviar) await bot.SendTextMessageAsync(chatId: userId, text: message, parseMode: ParseMode.Markdown);
      Console.WriteLine($"< {DateTime.Now} chatbot: {message}");
    }
    catch (ApiRequestException error)
    {
      Console.WriteLine($"< {DateTime.Now} chatbot: {message}");
      Console.WriteLine($"< {DateTime.Now} chatbot: Não foi possível enviar mensagem ao usuario");
      Console.WriteLine($"< {DateTime.Now} chatbot: {error.Message}");
    }
  }
}
