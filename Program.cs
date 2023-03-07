using dotenv.net;
using System.Linq;
using Microsoft.VisualBasic;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
// loads the environment variables configured in the .env file
DotEnv.Load();
// instantiates a new telegram bot api client with the specified token
var bot = new TelegramBotClient(System.Environment.GetEnvironmentVariable("TOKEN")!);
// opens and loads the list of users allowed to use the bot from a json file
var UsersFile = System.IO.File.Open("Users.json", FileMode.Open);
var users = System.Text.Json.JsonSerializer.Deserialize<List<telbot.Users>>(UsersFile);
// 
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
    else
    {
        // 
        await bot.SendTextMessageAsync(update.Message.From.Id, "Não estou programado para responder outras solicitações que não sejam mensagens");
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
    var xpto = (from id in users where id.Id == user.Id select id);
    if (xpto.Count() == 0)
    {
        await bot.SendTextMessageAsync(user.Id, "Eu não estou autorizado a te passar informações!");
        return;
    }
    // Print to console
    Console.WriteLine($"> {user.FirstName} escreveu: {text}");
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
    string[] args = text.Split(" ");
    if(args.Count() != 2)
    {
        await bot.SendTextMessageAsync(user.Id, "Verifique o formato da informação!");
        await bot.SendTextMessageAsync(user.Id, "Se tiver em dúvida de como usar o bot, digite /ajuda.");
        return;
    }
    var resposta = telbot.Temporary.executar(args[0], args[1]);
    if ((resposta.Count == 0) || (resposta is null))
    {
        await bot.SendTextMessageAsync(user.Id, "Não foi possível processar a sua solicitação!");
        await bot.SendTextMessageAsync(user.Id, "Solicite a informação para o monitor(a)");
        return;
    }
    if (resposta.Count == 2)
    {
        Console.WriteLine($"> {resposta[0].ToString()} enviado para: {user.FirstName}");
        await bot.SendTextMessageAsync(user.Id, resposta[0].ToString()!);
        return;
    }
    if (resposta.Count > 2)
    {
        Console.WriteLine("> Enviado arquivo");
        // await bot.SendPhotoAsync(user.Id, "");
        return;
    }
    await bot.SendTextMessageAsync(user.Id, "O que você está fazendo aqui?");
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
