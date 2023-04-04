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
  private List<telbot.Users> users;
  public Program()
  {
    // Identificador do administrador do BOT
    ID_ADM_BOT = Int64.Parse(System.Environment.GetEnvironmentVariable("ID_ADM_BOT")!);
    // opens and loads the list of users allowed to use the bot from a json file
    var UsersFile = System.IO.File.Open("Users.json", FileMode.Open);
    users = System.Text.Json.JsonSerializer.Deserialize<List<telbot.Users>>(UsersFile)!;
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
      // 
      await bot.SendTextMessageAsync(update.Message.From.Id, "Não estou programado para responder outras solicitações que não sejam mensagens");
    }
  }
  async Task HandleError(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken)
  {
    await ErrorReport(ID_ADM_BOT, "", "", exception);
    await Console.Error.WriteLineAsync(exception.Message);
    await Console.Error.WriteLineAsync(exception.StackTrace);
  }
  async Task HandleMessage(Message msg)
  {
    var user = msg.From;
    var text = msg.Text ?? string.Empty;
    if (user is null)
      return;
    Console.WriteLine($"> {user.FirstName} escreveu: {text}");
    try
    {
      var xpto = (from id in users where id.Id == user.Id select id).Single();
    }
    catch
    {
      await bot.SendTextMessageAsync(user.Id, "Eu não estou autorizado a te passar informações!");
      return;
    }
    // Print to console
    // When we get a command, we react accordingly
    if (text.StartsWith("/"))
    {
      await HandleCommand(user.Id, text);
      return;
    }
    if (!(text.Length > 0))
    {
      await bot.CopyMessageAsync(user.Id, user.Id, msg.MessageId);
      return;
    }
    text = text.ToLower();
    string[] args = text.Split(" ");
    if (args.Count() != 2)
    {
      await bot.SendTextMessageAsync(user.Id, "Verifique o formato da informação!");
      await bot.SendTextMessageAsync(user.Id, "Se tiver em dúvida de como usar o bot, digite /ajuda.");
      return;
    }
    var resposta = telbot.Temporary.executar(args[0], args[1]);
    if ((resposta.Count == 0) || (resposta is null))
    {
      await ErrorReport(user.Id, args[0], args[1], new Exception("Erro no script do SAP"));
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
            return;
          }
          await using Stream stream = System.IO.File.OpenRead(@$"C:\Users\ruan.camello\Documents\Temporario\{fatura}");
          await bot.SendDocumentAsync(user.Id, document: new Telegram.Bot.Types.InputFiles.InputOnlineFile(content: stream, fileName: fatura));
          stream.Dispose();
        }
      }
      catch (System.Exception error)
      {
        await ErrorReport(user.Id, args[0], args[1], error);
      }
      return;
    }
    if (args[0] == "leiturista" || args[0] == "roteiro" || args[0] == "historico")
    {
      try
      {
        telbot.Temporary.executar(resposta);
        await using Stream stream = System.IO.File.OpenRead(@$"C:\Users\ruan.camello\Documents\Temporario\temporario.png");
        await bot.SendPhotoAsync(user.Id, photo: new Telegram.Bot.Types.InputFiles.InputOnlineFile(content: stream));
        stream.Dispose();
        System.IO.File.Delete(@"C:\Users\ruan.camello\Documents\Temporario\temporario.png");
      }
      catch (System.Exception error)
      {
        await ErrorReport(user.Id, args[0], args[1], error);
      }
      return;
    }
    if (args[0] == "telefone" || args[0] == "coordenada" || args[0] == "localização" || args[0] == "contato")
    {
      await bot.SendTextMessageAsync(user.Id, resposta[0].ToString()!);
      return;
    }
    await bot.SendTextMessageAsync(user.Id, "Não entendi o comando, verifique se está correto!");
    return;
  }
  async Task HandleCommand(long userId, string command)
  {
    switch (command)
    {
      case "/start":
        await bot.SendTextMessageAsync(userId, "Seja bem vindo ao programa de automação de respostas do MestreRuan");
        await bot.SendTextMessageAsync(userId, "Digite o tipo de informação que deseja e depois o número da nota ou instalação. Por exemplo:");
        await bot.SendTextMessageAsync(userId, "leiturista 1012456598");
        await bot.SendTextMessageAsync(userId, "No momento temos as informações: TELEFONE, LOCALIZAÇÃO, LEITURISTA e FATURA");
        await bot.SendTextMessageAsync(userId, "Estou trabalhando para trazer mais funções em breve");
        break;
      case "/ajuda":
        await bot.SendTextMessageAsync(userId, "Digite o tipo de informação que deseja e depois o número da nota ou instalação. Por exemplo:");
        await bot.SendTextMessageAsync(userId, "leiturista 1012456598");
        await bot.SendTextMessageAsync(userId, "No momento temos as informações: TELEFONE, LOCALIZAÇÃO, LEITURISTA e FATURAS");
        await bot.SendTextMessageAsync(userId, "Estou trabalhando para trazer mais funções em breve");
        break;
    }
    await Task.CompletedTask;
  }
  async Task ErrorReport(long userId, string aplicacao, string informacao, Exception error)
  {
    await bot.SendTextMessageAsync(ID_ADM_BOT, $"Aplicação: {aplicacao}\nInformação: {informacao}\n\n{error.Message}");
    await bot.SendTextMessageAsync(userId, "Não foi possível processar a sua solicitação!");
    await bot.SendTextMessageAsync(userId, "Solicite a informação para o monitor(a)");
    return;
  }
}
