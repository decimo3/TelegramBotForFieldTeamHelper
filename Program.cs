using dotenv.net;
using System.Linq;
using Microsoft.VisualBasic;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
// using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

DotEnv.Load();

var bot = new TelegramBotClient(System.Environment.GetEnvironmentVariable("TOKEN"));
var UsersFile = System.IO.File.Open("Users.json", FileMode.Open);
var users = System.Text.Json.JsonSerializer.Deserialize<List<telbot.Users>>(UsersFile);
using var cts = new CancellationTokenSource();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool, so we use cancellation token
bot.StartReceiving(updateHandler: HandleUpdate, pollingErrorHandler: HandleError, cancellationToken: cts.Token);

// Tell the user the bot is online
Console.WriteLine("Start listening for updates. Press enter to stop");
Console.ReadLine();
// Send cancellation request to stop the bot
cts.Cancel();

// Each time a user interacts with the bot, this method is called
async Task HandleUpdate(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
{
  if (update.Type == UpdateType.Message)
  {
    // A message was received
    await HandleMessage(update.Message!);
  }
}

async Task HandleError(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken)
{
  await Console.Error.WriteLineAsync(exception.Message);
}


async Task HandleMessage(Message msg)
{
  var user = msg.From;
  var text = msg.Text ?? string.Empty;
  if (user is null)
    return;
  var xpto = from id in users where id.Id == user.Id select id;
  if (xpto.Count() == 0)
  {
    await bot.SendTextMessageAsync(user.Id, "Eu não estou autorizado a te passar informações!");
    return;
  }
  // Print to console
  Console.WriteLine($"{user.FirstName} wrote {text}");
  // When we get a command, we react accordingly
  if (text.StartsWith("/"))
  {
    await HandleCommand(user.Id, text);
  }
  else if (text.Length > 0)
  {
    string[] args = text.Split(" ");
    if(args.Count() != 2)
    {
      await bot.SendTextMessageAsync(user.Id, "Verifique o formato da informação!");
      await bot.SendTextMessageAsync(user.Id, "Se tiver em dúvida de como usar o bot, digite /ajuda.");
      return;
    }
    string? resposta = telbot.Temporary.executar(args[0], args[1]);
    // To preserve the markdown, we attach entities (bold, italic..)
    await bot.SendTextMessageAsync(user.Id, resposta);
  }
  else
  {   // This is equivalent to forwarding, without the sender's name
    await bot.CopyMessageAsync(user.Id, user.Id, msg.MessageId);
  }
}

async Task HandleCommand(long userId, string command)
{
  switch (command)
  {
    case "/start":
      await bot.SendTextMessageAsync(userId, "Seja bem vindo ao programa de automação de respostas do MestreRuan");
      await bot.SendTextMessageAsync(userId, "Digite o tipo de informação que deseja e depois o número da nota ou instalação. Por exemplo:");
      await bot.SendTextMessageAsync(userId, "leiturista 1012456598");
      await bot.SendTextMessageAsync(userId, "No momento temos as informações: TELEFONE, LOCALIZAÇÃO");
      await bot.SendTextMessageAsync(userId, "Estou trabalhando para trazer mais funções em breve");
      break;
    case "/ajuda":
      await bot.SendTextMessageAsync(userId, "Digite o tipo de informação que deseja e depois o número da nota ou instalação. Por exemplo:");
      await bot.SendTextMessageAsync(userId, "leiturista 1012456598");
      await bot.SendTextMessageAsync(userId, "No momento temos as informações: TELEFONE, LOCALIZAÇÃO");
      await bot.SendTextMessageAsync(userId, "Estou trabalhando para trazer mais funções em breve");
      break;
  }
  await Task.CompletedTask;
}
