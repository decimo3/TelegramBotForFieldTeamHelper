namespace telbot.handle;

using telbot.Helpers;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
public class HandleMessage
{
  private readonly string errorMensagem = "Não foi possível responder a sua solicitação. Tente novamente!";
  private ITelegramBotClient bot;
  public HandleMessage(ITelegramBotClient bot)
  {
    this.bot = bot;
  }
  public async Task sendTextMesssageWraper(long userId, string message, bool enviar=true, bool exibir=true)
  {
    ConsoleWrapper.Debug(Entidade.Chatbot, message);
    try
    {
      if(enviar) await bot.SendTextMessageAsync(chatId: userId, text: message, parseMode: ParseMode.Markdown);
      if(exibir) Console.WriteLine($"< {DateTime.Now} chatbot: {message}");
    }
    catch (Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Messenger, erro);
      Recovery.ErrorSendMessageReport(new errorReport(){
        identificador = userId,
        mensagem = message
      });
    }
  }
  public async Task<string> SendDocumentAsyncWraper(long id, Stream stream, string filename)
  {
    try
    {
      var documento = await bot.SendDocumentAsync(id, document: new Telegram.Bot.Types.InputFiles.InputOnlineFile(content: stream, fileName: filename));
      if(documento.Document == null) throw new Exception(errorMensagem);
      return documento.Document.FileId;
    }
    catch (Exception erro)
    {
      stream.Position = 0;
      ConsoleWrapper.Error(Entidade.Messenger, erro);
      Recovery.ErrorSendMessageReport(new errorReport(){
        identificador = id,
        mensagem = errorMensagem
      });
      return String.Empty;
    }
  }
  public async Task SendCoordinateAsyncWraper(long id, string mapLink)
  {
    ConsoleWrapper.Debug(Entidade.Messenger, mapLink);
    try
    {
      var re = new System.Text.RegularExpressions.Regex(@"-[0-9]{1,2}[\.|,][0-9]{5,}");
      var loc = re.Matches(mapLink);
      await bot.SendLocationAsync(id, Double.Parse(loc[0].Value.Replace('.', ',')), Double.Parse(loc[1].Value.Replace('.', ',')));
      Console.WriteLine($"< {DateTime.Now} chatbot: Enviada coordenadas da instalação: {loc[0].Value},{loc[1].Value}");
    }
    catch (Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Messenger, erro);
      Recovery.ErrorSendMessageReport(new errorReport(){
        identificador = id,
        mensagem = errorMensagem
      });
    }
  }
  public async Task ErrorReport(long id, Exception error, telbot.models.Request? request=null, string? SAPerrorMessage=null)
  {
    if(SAPerrorMessage is not null) await sendTextMesssageWraper(id, SAPerrorMessage);
    await sendTextMesssageWraper(id, "Não foi possível processar a sua solicitação!");
    await sendTextMesssageWraper(id, "Solicite a informação para o monitor(a)");
    if(request is null) Database.inserirRelatorio(new logsModel(id, string.Empty, 0, false, DateTime.Now));
    else Database.inserirRelatorio(new logsModel(id, request.aplicacao, request.informacao, false, request.received_at));
    return;
  }
  public async Task<string> SendPhotoAsyncWraper(long id, Stream stream)
  {
    try
    {
      var photo = await bot.SendPhotoAsync(id, photo: new Telegram.Bot.Types.InputFiles.InputOnlineFile(content: stream));
      if(photo.Photo is null) throw new Exception(errorMensagem);
      return photo.Photo.First().FileId;
    }
    catch (Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Messenger, erro);
      stream.Position = 0;
      Recovery.ErrorSendMessageReport(new errorReport(){
        identificador = id,
        mensagem = errorMensagem
      });
      return String.Empty;
    }
  }
  public async Task RequestContact(long id)
  {
    try
    {
      await sendTextMesssageWraper(id, "É necessário informar o seu telefone para continuar!");
      await sendTextMesssageWraper(id, "Não será mais autorizado sem cadastrar o número de telefone");
      var msg = "Clique no botão abaixo para enviar o seu número!";
      var requestReplyKeyboard = new ReplyKeyboardMarkup( new[] { KeyboardButton.WithRequestContact("Enviar meu número de telefone") });
      await bot.SendTextMessageAsync(chatId: id, text: msg, replyMarkup: requestReplyKeyboard);
      Console.WriteLine($"< {DateTime.Now} chatbot: {msg}");
    }
    catch (Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Messenger, erro);
      Recovery.ErrorSendMessageReport(new errorReport(){
        identificador = id
      });
    }
    return;
  }
  public async Task RemoveRequest(long id, string tel)
  {
    try
    {
      var msg = $"Telefone {tel} cadastrado! Agora serás atendido normalmente!";
      var requestReplyKeyboard = new ReplyKeyboardRemove();
      await bot.SendTextMessageAsync(chatId: id, text: msg, replyMarkup: requestReplyKeyboard);
      Console.WriteLine($"< {DateTime.Now} chatbot: {msg}");
    }
    catch (Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Messenger, erro);
      if(!tel.StartsWith('+')) tel = '+' + tel;
      Recovery.ErrorSendMessageReport(new errorReport(){
        identificador = id,
        mensagem = tel
      });
    }
    return;
  }
  public async Task<string> SendVideoAsyncWraper(long id, Stream stream)
  {
    try
    {
      var video = await bot.SendVideoAsync(id, video: new Telegram.Bot.Types.InputFiles.InputOnlineFile(content: stream));
      if(video.Video is null) throw new Exception(errorMensagem);
      return video.Video.FileId;
    }
    catch (Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Messenger, erro);
      stream.Position = 0;
      Recovery.ErrorSendMessageReport(new errorReport(){
        identificador = id,
        mensagem = errorMensagem
      });
      return String.Empty;
    }
  }
  public async Task SendVideoAsyncWraper(long id, string media_id)
  {
    try
    {
      await bot.SendVideoAsync(id, video: media_id);
    }
    catch (Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Messenger, erro);
      Recovery.ErrorSendMessageReport(new errorReport(){
        identificador = id,
        mensagem = errorMensagem
      });
    }
  }
  public async Task SendPhotoAsyncWraper(long id, string media_id)
  {
    try
    {
      await bot.SendPhotoAsync(id, photo: media_id);
    }
    catch (Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Messenger, erro);
      Recovery.ErrorSendMessageReport(new errorReport(){
        identificador = id,
        mensagem = errorMensagem
      });
    }
  }
  public async Task SendDocumentAsyncWraper(long id, string media_id)
  {
    try
    {
      await bot.SendDocumentAsync(id, document: media_id);
    }
    catch (Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Messenger, erro);
      Recovery.ErrorSendMessageReport(new errorReport(){
        identificador = id,
        mensagem = errorMensagem
      });
    }
  }
}