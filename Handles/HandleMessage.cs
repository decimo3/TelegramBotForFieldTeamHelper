namespace telbot.handle;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.ReplyMarkups;
public class HandleMessage
{
  private ITelegramBotClient bot;
  public HandleMessage(ITelegramBotClient bot)
  {
    this.bot = bot;
  }
    public async Task sendTextMesssageWraper(long userId, string message, bool enviar=true)
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
  public async Task SendDocumentAsyncWraper(long id, Stream stream, string filename)
  {
    await bot.SendDocumentAsync(id, document: new Telegram.Bot.Types.InputFiles.InputOnlineFile(content: stream, fileName: filename));
  }
  public async Task ErrorReport(long id, Exception error, telbot.models.Request? request=null, string? SAPerrorMessage=null)
  {
    if(SAPerrorMessage is not null) await sendTextMesssageWraper(id, SAPerrorMessage);
    await sendTextMesssageWraper(id, "Não foi possível processar a sua solicitação!");
    await sendTextMesssageWraper(id, "Solicite a informação para o monitor(a)");
    if(request is null) Database.inserirRelatorio(new logsModel(id, string.Empty, string.Empty, false, DateTime.Now));
    else Database.inserirRelatorio(new logsModel(id, request.aplicacao, request.informacao, false, request.received_at));
    return;
  }
  public async Task SendPhotoAsyncWraper(long id, Stream stream)
  {
    await bot.SendPhotoAsync(id, photo: new Telegram.Bot.Types.InputFiles.InputOnlineFile(content: stream));
  }
  public async Task RequestContact(long id)
  {
    await sendTextMesssageWraper(id, "É necessário informar o seu telefone para continuar!");
    await sendTextMesssageWraper(id, "Não será mais autorizado sem cadastrar o número de telefone");
    var msg = "Clique no botão abaixo para enviar o seu número!";
    var requestReplyKeyboard = new ReplyKeyboardMarkup( new[] { KeyboardButton.WithRequestContact("Enviar meu número de telefone") });
    await bot.SendTextMessageAsync(chatId: id, text: msg, replyMarkup: requestReplyKeyboard);
    Console.WriteLine($"< {DateTime.Now} chatbot: {msg}");
    return; 
  }
  public async Task RemoveRequest(long id, string tel)
  {
    var msg = $"Telefone {tel} cadastrado! Agora serás atendido normalmente!";
    await bot.SendTextMessageAsync(chatId: id, text: msg, replyMarkup: new ReplyKeyboardRemove());
    Console.WriteLine($"< {DateTime.Now} chatbot: {msg}");
    return;
  }
}