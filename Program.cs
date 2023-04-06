using System.Linq;
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
  // instantiates a new telegram bot api client with the specified token
  private TelegramBotClient bot;
  public Program()
  {
    // Identificador do administrador do BOT
    ID_ADM_BOT = Int64.Parse(System.Environment.GetEnvironmentVariable("ID_ADM_BOT")!);
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
    if (update.Type == UpdateType.Message)
    {
      // A message was received
      await HandleMessage(update.Message!);
    }
    else
    {
      Console.WriteLine(update.ToString());
      await ErrorReport(ID_ADM_BOT, string.Empty, string.Empty, new InvalidOperationException(update.ToString()));
      return;
    }
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
    UsersModel user;
    string text = message.Text;
    try
    {
      user = Database.recuperarUsuario(message.From.Id);
    }
    catch
    {
      await bot.SendTextMessageAsync(message.From.Id, "Eu não estou autorizado a te passar informações!");
      await bot.SendTextMessageAsync(message.From.Id, $"Seu identificador do Telegram é {message.From.Id}.");
      await bot.SendTextMessageAsync(message.From.Id, "Informe ao seu supervisor esse id para ter acesso ao BOT");
      return;
    }
    // verifica se o cadastro tem mais de 60 dias desde a atualização
    if(System.DateTime.Compare(user.update_at, user.update_at.AddDays(30)) > 0)
    {
      await bot.SendTextMessageAsync(message.From.Id, "Sua autorização expirou e não posso mais te passar informações");
      await bot.SendTextMessageAsync(message.From.Id, "Solicite a autorização novamente para o seu supervisor!");
      await bot.SendTextMessageAsync(message.From.Id, $"Seu identificador do Telegram é {message.From.Id}.");
      return;
    }
    // Verifica se o cadastro está perto de expirar (7 dias antes) e avisa
    if(System.DateTime.Compare(user.update_at, user.update_at.AddDays(23)) > 0)
    {
      await bot.SendTextMessageAsync(message.From.Id, "Sua autorização está quase expirando!");
      await bot.SendTextMessageAsync(message.From.Id, "Solicite a **atualização** para o seu supervisor!");
      await bot.SendTextMessageAsync(message.From.Id, $"Seu identificador do Telegram é {message.From.Id}.");
    }
    // Print to console
    Console.WriteLine($"> {user.id} escreveu: {text}");
    if (!(text.Length > 0))
    {
      await bot.SendTextMessageAsync(user.id, "Não estou programado para responder a solicitações que não sejam mensagens de texto!");
      await bot.SendTextMessageAsync(user.id, "Verifique o formato da informação e tente novamente da forma correta!");
      await bot.SendTextMessageAsync(user.id, "Se tiver em dúvida de como usar o bot, digite /ajuda.");
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
      await bot.SendTextMessageAsync(user.id, "Verifique o formato da informação e tente novamente da forma correta!");
      await bot.SendTextMessageAsync(user.id, "Se tiver em dúvida de como usar o bot, digite /ajuda.");
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
      await bot.SendTextMessageAsync(user.id, "Verifique o formato da informação e tente novamente da forma correta!");
      await bot.SendTextMessageAsync(user.id, "Se tiver em dúvida de como usar o bot, digite /ajuda.");
      return;
    }
    if (args[0] == "autorizar")
    {
      if(!user.has_privilege)
      {
        await bot.SendTextMessageAsync(user.id, "Você não tem permissão para autorizar usuários!");
        Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], false));
        return;
      }
      if(Int64.TryParse(args[1], out long id))
      {
        Database.inserirUsuario(new UsersModel(id, user.id));
        await bot.SendTextMessageAsync(user.id, "Usuário autorizado com sucesso!");
        Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], true));
      }
      else
      {
        await bot.SendTextMessageAsync(user.id, "O identificador do usuário não é válido!");
        Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], false));
      }
      return;
    }
    if (args[0] == "atualizar")
    {
      if(!user.has_privilege)
      {
        await bot.SendTextMessageAsync(user.id, "Você não tem permissão para atualizar usuários!");
        Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], false));
        return;
      }
      if(Int64.TryParse(args[1], out long id))
      {
        if(Database.atualizarUsuario(id, user.id))
        {
          await bot.SendTextMessageAsync(user.id, "Usuário atualizado com sucesso!");
          Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], true));
        }
        else
        {
          await bot.SendTextMessageAsync(user.id, "Houve um problema em promover o usuário");
          await bot.SendTextMessageAsync(user.id, "Verifique as informações e tente novamente");
          Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], false));
        }
      }
      else
      {
        await bot.SendTextMessageAsync(user.id, "O identificador do usuário não é válido!");
        Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], false));
      }
      return;
    }
    if (args[0] == "promover")
    {
      if(user.id != ID_ADM_BOT)
      {
        await bot.SendTextMessageAsync(user.id, "Você não tem permissão para promover usuários!");
        Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], false));
        return;
      }
      if(Int64.TryParse(args[1], out long id))
      {
        if(Database.promoverUsuario(id, user.id))
        {
          await bot.SendTextMessageAsync(user.id, "Usuário promovido com sucesso!");
          Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], true));
        }
        else
        {
          await bot.SendTextMessageAsync(user.id, "Houve um problema em promover o usuário");
          await bot.SendTextMessageAsync(user.id, "Verifique as informações e tente novamente");
          Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], false));
        }
      }
      else
      {
        await bot.SendTextMessageAsync(user.id, "O identificador do usuário não é válido!");
        Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], false));
      }
      return;
    }
    var resposta = telbot.Temporary.executar(args[0], args[1]);
    if ((resposta.Count == 0) || (resposta is null))
    {
      await ErrorReport(user.id, args[0], args[1], new Exception("Erro no script do SAP"));
      return;
    }
    if (args[0] == "fatura" || args[0] == "debito")
    {
      try
      {
        foreach (string fatura in resposta)
        {
          if (fatura == "None" || fatura == null || fatura == "")
          {
            Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], true));
            return;
          }
          await using Stream stream = System.IO.File.OpenRead(@$"{Temporary.USER_PATH}\Documents\Temporario\{fatura}");
          await bot.SendDocumentAsync(user.id, document: new Telegram.Bot.Types.InputFiles.InputOnlineFile(content: stream, fileName: fatura));
          stream.Dispose();
        }
      }
      catch (System.Exception error)
      {
        await ErrorReport(user.id, args[0], args[1], error);
      }
      return;
    }
    if (args[0] == "leiturista" || args[0] == "roteiro" || args[0] == "historico")
    {
      try
      {
        telbot.Temporary.executar(resposta);
        await using Stream stream = System.IO.File.OpenRead(@$"{Temporary.USER_PATH}\Documents\Temporario\temporario.png");
        await bot.SendPhotoAsync(user.id, photo: new Telegram.Bot.Types.InputFiles.InputOnlineFile(content: stream));
        stream.Dispose();
        System.IO.File.Delete(@$"{Temporary.USER_PATH}\Documents\Temporario\temporario.png");
        Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], true));
      }
      catch (System.Exception error)
      {
        await ErrorReport(user.id, args[0], args[1], error);
      }
      return;
    }
    if (args[0] == "telefone" || args[0] == "coordenada" || args[0] == "localização" || args[0] == "contato")
    {
      await bot.SendTextMessageAsync(user.id, resposta[0].ToString()!);
      Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], true));
      return;
    }
    await bot.SendTextMessageAsync(user.id, "Não entendi o comando, verifique se está correto!");
    Database.inserirRelatorio(new logsModel(user.id, args[0], args[1], false));
    return;
  }
  async Task HandleCommand(long userId, string command)
  {
    switch (command)
    {
      case "/start":
        await bot.SendTextMessageAsync(userId, "Seja bem vindo ao programa de automação de respostas do MestreRuan");
        await bot.SendTextMessageAsync(userId, "Se tiver em dúvida de como usar o bot, digite /ajuda.");
        break;
      case "/ajuda":
        await bot.SendTextMessageAsync(userId, "Digite o tipo de informação que deseja e depois o número da nota ou instalação. Por exemplo:");
        await bot.SendTextMessageAsync(userId, "leiturista 1012456598");
        await bot.SendTextMessageAsync(userId, "No momento temos as informações: TELEFONE, LOCALIZAÇÃO, LEITURISTA e FATURAS");
        await bot.SendTextMessageAsync(userId, "Estou trabalhando para trazer mais funções em breve");
        break;
      case "/ping":
        await bot.SendTextMessageAsync(userId, "Estou de prontidão aguardando as solicitações! (^.^)");
        break;
      default:
        await bot.SendTextMessageAsync(userId, "Comando solicitado não foi programado! Verifique e tente um válido");
        break;
    }
    await Task.CompletedTask;
  }
  async Task ErrorReport(long userId, string aplicacao, string informacao, Exception error)
  {
    await bot.SendTextMessageAsync(ID_ADM_BOT, $"Aplicação: {aplicacao}\nInformação: {informacao}\n\n{error.Message}");
    await bot.SendTextMessageAsync(userId, "Não foi possível processar a sua solicitação!");
    await bot.SendTextMessageAsync(userId, "Solicite a informação para o monitor(a)");
    Database.inserirRelatorio(new logsModel(userId, aplicacao, informacao, false));
    return;
  }
}
