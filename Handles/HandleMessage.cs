namespace telbot.handle;
using telbot.Services;
using telbot.Helpers;
using telbot.models;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Logging;
public class HandleMessage
{
  private ITelegramBotClient bot;
  private static HandleMessage _instance;
  private static readonly Object _lock = new();
  private readonly ILogger logger = Logger.GetInstance<HandleMessage>();
  private readonly string errorMensagem = "Não foi possível responder a sua solicitação.";
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
    try
    {
      logger.LogDebug(message);
      ParseMode? parsemode = markdown ? ParseMode.Markdown : null;
      if(enviar) await bot.SendTextMessageAsync(chatId: userId, text: message, parseMode: parsemode);
      if(exibir) logger.LogInformation(message);
    }
    catch (Telegram.Bot.Exceptions.ApiRequestException erro)
    {
      logger.LogError(erro, "O usuário {user}", userId);
    }
    catch
    {
      throw;
    }
  }
  public async Task<String?> SendDocumentAsyncWraper(long id, Stream stream, string filename)
  {
    try
    {
      var document = new Telegram.Bot.Types.InputFiles.InputOnlineFile(content: stream, fileName: filename);
      var documento = await bot.SendDocumentAsync(id, document: document);
      if(documento.Document == null) throw new Exception(errorMensagem);
      return documento.Document.FileId;
    }
    catch (Telegram.Bot.Exceptions.ApiRequestException erro)
    {
      logger.LogError(erro, "O usuário {user}", id);
      return null;
    }
    catch
    {
      throw;
    }
  }
  public async Task SendCoordinateAsyncWraper(Int64 id, Double latitude, Double longitude)
  {
    try
    {
      await bot.SendLocationAsync(id, latitude, longitude);
      logger.LogInformation("Enviada coordenadas: {latitude},{longitude}", latitude, longitude);
    }
    catch (Telegram.Bot.Exceptions.ApiRequestException erro)
    {
      logger.LogError(erro, "O usuário {user}", id);
    }
    catch
    {
      throw;
    }
  }
  public async Task ErrorReport(Exception error, logsModel request)
  {
    String texto;
    var regex = new System.Text.RegularExpressions.Regex("^([0-9]{3}):");
    logger.LogError(error, "Não foi possível processar a solicitação: ");
    var match = regex.Match(error.Message);
    if(match.Success)
    {
      texto = error.Message[5..];
      request.status = Int32.Parse(match.Value[..3]);
    }
    else
    {
      texto = error.Message;
      if(request.status < 400) request.status = 500;
    }
    if(request.identifier > 10)
    {
      if(!String.IsNullOrEmpty(texto))
        await sendTextMesssageWraper(request.identifier, texto);
      await sendTextMesssageWraper(request.identifier, "Não foi possível processar a sua solicitação!");
      await sendTextMesssageWraper(request.identifier, "Solicite a informação para o monitor(a)");
    }
    request.response_at = DateTime.Now;
    Database.GetInstance().AlterarSolicitacao(request);
  }
  public void SucessReport(logsModel request)
  {
    request.response_at = DateTime.Now;
    request.status = 200;
    Database.GetInstance().AlterarSolicitacao(request);
  }
  public async Task<String?> SendPhotoAsyncWraper(long id, Stream stream)
  {
    try
    {
      var photograph = new Telegram.Bot.Types.InputFiles.InputOnlineFile(content: stream);
      var photo = await bot.SendPhotoAsync(id, photo: photograph);
      if(photo.Photo is null) throw new Exception(errorMensagem);
      return photo.Photo.First().FileId;
    }
    catch (Telegram.Bot.Exceptions.ApiRequestException erro)
    {
      logger.LogError(erro, "O usuário {user}", id);
      return null;
    }
    catch
    {
      throw;
    }
  }
  public async Task RequestContact(long id)
  {
      await sendTextMesssageWraper(id, "É necessário informar o seu telefone para continuar!");
      await sendTextMesssageWraper(id, "Não será mais autorizado sem cadastrar o número de telefone");
      var msg = "Clique no botão abaixo para enviar o seu número!";
      var keys = new[] { KeyboardButton.WithRequestContact("Enviar meu número de telefone") };
      var requestReplyKeyboard = new ReplyKeyboardMarkup(keys);
      await bot.SendTextMessageAsync(chatId: id, text: msg, replyMarkup: requestReplyKeyboard);
      logger.LogInformation(msg);
  }
  public async Task RemoveRequest(long id, Int64 tel)
  {
      var msg = $"Telefone {tel} cadastrado! Agora serás atendido normalmente!";
      var requestReplyKeyboard = new ReplyKeyboardRemove();
      await bot.SendTextMessageAsync(chatId: id, text: msg, replyMarkup: requestReplyKeyboard);
      logger.LogInformation(msg);
  }
  public async Task<String?> SendVideoAsyncWraper(long id, Stream stream)
  {
    try
    {
      var videoclip = new Telegram.Bot.Types.InputFiles.InputOnlineFile(content: stream);
      var video = await bot.SendVideoAsync(id, video: videoclip);
      if(video.Video is null) throw new Exception(errorMensagem);
      return video.Video.FileId;
    }
    catch (Telegram.Bot.Exceptions.ApiRequestException erro)
    {
      logger.LogError(erro, "O usuário {user}", id);
      return null;
    }
    catch
    {
      throw;
    }
  }
  public async Task SendVideoAsyncWraper(long id, string media_id, string? legenda = null)
  {
    try
    {
      await bot.SendVideoAsync(id, video: media_id, caption: legenda);
    }
    catch (Telegram.Bot.Exceptions.ApiRequestException erro)
    {
      logger.LogError(erro, "O usuário {user}", id);
    }
    catch
    {
      throw;
    }
  }
  public async Task SendPhotoAsyncWraper(long id, string media_id, string? legenda = null)
  {
    try
    {
      await bot.SendPhotoAsync(id, photo: media_id, caption: legenda);
    }
    catch (Telegram.Bot.Exceptions.ApiRequestException erro)
    {
      logger.LogError(erro, "O usuário {user}", id);
    }
    catch
    {
      throw;
    }
  }
  public async Task SendDocumentAsyncWraper(long id, string media_id, string? legenda = null)
  {
    try
    {
      await bot.SendDocumentAsync(id, document: media_id, caption: legenda);
    }
    catch (Telegram.Bot.Exceptions.ApiRequestException erro)
    {
      logger.LogError(erro, "O usuário {user}", id);
    }
    catch
    {
      throw;
    }
  }
}