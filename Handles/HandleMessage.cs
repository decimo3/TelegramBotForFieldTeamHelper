namespace telbot.handle;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
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
    Database.inserirRelatorio(new logsModel(id, request.aplicacao, request.informacao, false));
    return;
  }
  public async Task SendPhotoAsyncWraper(long id, Stream stream)
  {
    await bot.SendPhotoAsync(id, photo: new Telegram.Bot.Types.InputFiles.InputOnlineFile(content: stream));
  }
}