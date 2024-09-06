namespace telbot.handle;
using telbot.Services;
using telbot.Helpers;
using telbot.models;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
public class HandleMessage
{
  private static HandleMessage _instance;
  private static readonly Object _lock = new();
  private readonly string errorMensagem = "Não foi possível responder a sua solicitação. Tente novamente!";
  private ITelegramBotClient bot;
  private HandleMessage(ITelegramBotClient bot)
  {
    this.bot = bot;
  }
  // Public static method to get the singleton instance
  public static HandleMessage GetInstance(ITelegramBotClient? bot = null)
  {
    lock (_lock)
    {
      if (_instance == null)
      {
        if (bot == null)
        {
          throw new InvalidOperationException("HandleMessage must be instantiated with a valid ITelegramBotClient.");
        }
        _instance = new HandleMessage(bot);
      }
      return _instance;
    }
  }
  public async Task sendTextMesssageWraper(long userId, string message, bool enviar=true, bool exibir=true, bool markdown=true)
  {
    ConsoleWrapper.Debug(Entidade.Chatbot, message);
    try
    {
      ParseMode? parsemode = markdown ? ParseMode.Markdown : null;
      if(enviar) await bot.SendTextMessageAsync(chatId: userId, text: message, parseMode: parsemode);
      if(exibir) ConsoleWrapper.Write(Entidade.Messenger, message);
    }
    catch (Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Messenger, erro);
    }
  }
  public async Task<string> SendDocumentAsyncWraper(long id, Stream stream, string filename)
  {
    try
    {
      var document = new Telegram.Bot.Types.InputFiles.InputOnlineFile(content: stream, fileName: filename);
      var documento = await bot.SendDocumentAsync(id, document: document);
      if(documento.Document == null) throw new Exception(errorMensagem);
      return documento.Document.FileId;
    }
    catch (Exception erro)
    {
      stream.Position = 0;
      ConsoleWrapper.Error(Entidade.Messenger, erro);
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
      var lat = Double.Parse(loc[0].Value.Replace('.', ','));
      var lon = Double.Parse(loc[1].Value.Replace('.', ','));
      await bot.SendLocationAsync(id, lat, lon);
      ConsoleWrapper.Write(Entidade.Messenger, $"Enviada coordenadas da instalação: {lat},{lon}");
    }
    catch (Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Messenger, erro);
    }
  }
  public async Task ErrorReport(long id, Exception error, logsModel? request)
  {
    if(id > 10)
    {
      if(!String.IsNullOrEmpty(error.Message))
        await sendTextMesssageWraper(id, error.Message);
      await sendTextMesssageWraper(id, "Não foi possível processar a sua solicitação!");
      await sendTextMesssageWraper(id, "Solicite a informação para o monitor(a)");
    }
    ConsoleWrapper.Error(Entidade.CookerAsync, error);
    if(request == null) return;
    if(request.status < 400) request.status = 500;
    request.response_at = DateTime.Now;
    Database.GetInstance().AlterarSolicitacao(request);
  }
  public void SucessReport(logsModel request)
  {
    request.response_at = DateTime.Now;
    request.status = 200;
    Database.GetInstance().AlterarSolicitacao(request);
  }
  public async Task<string> SendPhotoAsyncWraper(long id, Stream stream)
  {
    try
    {
      var photograph = new Telegram.Bot.Types.InputFiles.InputOnlineFile(content: stream);
      var photo = await bot.SendPhotoAsync(id, photo: photograph);
      if(photo.Photo is null) throw new Exception(errorMensagem);
      return photo.Photo.First().FileId;
    }
    catch (Exception erro)
    {
      stream.Position = 0;
      ConsoleWrapper.Error(Entidade.Messenger, erro);
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
      var keys = new[] { KeyboardButton.WithRequestContact("Enviar meu número de telefone") };
      var requestReplyKeyboard = new ReplyKeyboardMarkup(keys);
      await bot.SendTextMessageAsync(chatId: id, text: msg, replyMarkup: requestReplyKeyboard);
      ConsoleWrapper.Write(Entidade.Messenger, msg);
    }
    catch (Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Messenger, erro);
    }
    return;
  }
  public async Task RemoveRequest(long id, Int64 tel)
  {
    try
    {
      var msg = $"Telefone {tel} cadastrado! Agora serás atendido normalmente!";
      var requestReplyKeyboard = new ReplyKeyboardRemove();
      await bot.SendTextMessageAsync(chatId: id, text: msg, replyMarkup: requestReplyKeyboard);
      ConsoleWrapper.Write(Entidade.Messenger, msg);
    }
    catch (Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Messenger, erro);
    }
    return;
  }
  public async Task<string> SendVideoAsyncWraper(long id, Stream stream)
  {
    try
    {
      var videoclip = new Telegram.Bot.Types.InputFiles.InputOnlineFile(content: stream);
      var video = await bot.SendVideoAsync(id, video: videoclip);
      if(video.Video is null) throw new Exception(errorMensagem);
      return video.Video.FileId;
    }
    catch (Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Messenger, erro);
      stream.Position = 0;
      return String.Empty;
    }
  }
  public async Task SendVideoAsyncWraper(long id, string media_id, string? legenda = null)
  {
    try
    {
      await bot.SendVideoAsync(id, video: media_id, caption: legenda);
    }
    catch (Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Messenger, erro);
    }
  }
  public async Task SendPhotoAsyncWraper(long id, string media_id, string? legenda = null)
  {
    try
    {
      await bot.SendPhotoAsync(id, photo: media_id, caption: legenda);
    }
    catch (Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Messenger, erro);
    }
  }
  public async Task SendDocumentAsyncWraper(long id, string media_id, string? legenda = null)
  {
    try
    {
      await bot.SendDocumentAsync(id, document: media_id, caption: legenda);
    }
    catch (Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Messenger, erro);
    }
  }
}