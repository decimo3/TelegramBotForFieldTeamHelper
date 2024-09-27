using telbot.Helpers;
using telbot.models;
using telbot.Services;
using Microsoft.Extensions.Logging;
namespace telbot.handle;
public class HandleAsynchronous
{
  private static readonly System.Text.RegularExpressions.Regex regex = new("^[0-9]{3}:");
  private static Stream ExecutarImg(String table)
  {
    var argumentos = new String[] { "\"" + table + "\"" };
    var resposta_txt = Executor.Executar("img.exe", argumentos, true);
    if(String.IsNullOrEmpty(resposta_txt))
    {
      throw new IndexOutOfRangeException(
        "503: Não foi recebida nenhuma resposta do `IMG2CSV`!");
    }
    if(regex.Match(resposta_txt).Success)
    {
      throw new InvalidOperationException(resposta_txt);
    }
    var bytearray = Convert.FromBase64String(resposta_txt);
    return new MemoryStream(bytearray);
  }
  private static String ExecutarSap(String solicitation, Int64 information, Int32 instance)
  {
    var argumentos = new String[] {
      solicitation,
      information.ToString(),
      instance.ToString()
    };
    var resposta_txt = Executor.Executar("sap.exe", argumentos, true);
    if(String.IsNullOrEmpty(resposta_txt))
    {
      throw new NullReferenceException(
        $"503: Não foi recebida nenhuma resposta do `SAP_BOT`!");
    }
    if(regex.Match(resposta_txt).Success)
    {
      throw new InvalidOperationException(resposta_txt);
    }
    return resposta_txt;
  }
  public static async Task Waiter(Int64 identificador, String mensagem, DateTime received_at)
  {
    var database = Database.GetInstance();
    var telegram = HandleMessage.GetInstance();
    var logger = Logger.GetInstance<HandleAsynchronous>();
    logger.LogInformation("{identificador} escreveu: {mensagem}", identificador, mensagem);
    var request = Validador.isRequest(mensagem);
    if (request is null)
    {
      await telegram.sendTextMesssageWraper(identificador,
        "Verifique o formato da informação e tente novamente da forma correta!");
      await telegram.sendTextMesssageWraper(identificador,
        "Se tiver em dúvida de como usar o bot, digite /ajuda.");
      return;
    }
    request.identifier = identificador;
    request.received_at = received_at;
    database.InserirSolicitacao(request);
  }
  public static async Task Chief()
  {
    var bot = HandleMessage.GetInstance();
    var cfg = Configuration.GetInstance();
    var database = Database.GetInstance();
    var logger = Logger.GetInstance<HandleAsynchronous>();
    var instanceControl = new bool[cfg.SAP_INSTANCIA];
    var semaphore = new SemaphoreSlim(cfg.SAP_INSTANCIA);
    while (true)
    {
      await Task.Delay(cfg.TASK_DELAY);
      var solicitacoes = database.RecuperarSolicitacao();
      var solicitacao_texto = System.Text.Json.JsonSerializer.Serialize<List<logsModel>>(solicitacoes);
      logger.LogDebug(solicitacao_texto);
      if(!solicitacoes.Any()) continue;
      var tasks = new List<Task>();
      foreach (var solicitacao in solicitacoes)
      {
        await semaphore.WaitAsync();
        int instanceNumber = -1;
        lock (instanceControl)
        {
          for (int i = 0; i < instanceControl.Length; i++)
          {
            // If the instance is available
            if (!instanceControl[i])
            {
              // Mark it as occupied
              instanceControl[i] = true;
              // Set the instance number (1-based index)
              instanceNumber = i + 1;
              break;
            }
          }
        }
        // In case no instance was found
        if (instanceNumber == -1)
        {
          semaphore.Release();
          continue;
        }
        solicitacao.instance = instanceNumber;
        tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await Cooker(solicitacao);
                        }
                        finally
                        {
                          lock (instanceControl)
                          {
                            // Mark the instance as available again (1-based index)
                            instanceControl[instanceNumber - 1] = false;
                          }
                          // Release the semaphore slot
                            semaphore.Release();
                        }
                    }));
      }
      await Task.WhenAll(tasks);
    }
  }
  public static async Task Cooker(logsModel solicitacao)
  {
      var bot = HandleMessage.GetInstance();
      var cfg = Configuration.GetInstance();
      var database = Database.GetInstance();
      var logger = Logger.GetInstance<HandleAsynchronous>();
    try
    {
      var solicitacao_texto = System.Text.Json.JsonSerializer.Serialize<logsModel>(solicitacao);
      logger.LogDebug(solicitacao_texto);
      if(solicitacao.typeRequest != TypeRequest.gestao && solicitacao.typeRequest != TypeRequest.comando)
      {
        if(!cfg.IS_DEVELOPMENT && solicitacao.received_at.AddMilliseconds(cfg.SAP_ESPERA) < DateTime.Now)
        {
          solicitacao.status = 408;
          var erro = new Exception("A sua solicitação expirou!");
          await bot.ErrorReport(erro, solicitacao);
          return;
        }
      }
      var user = database.RecuperarUsuario(solicitacao.identifier) ??
        throw new NullReferenceException("Usuario não foi encontrado!");
      switch (solicitacao.typeRequest)
      {
        case TypeRequest.gestao:
        {
          await Manager.HandleManager(solicitacao, user);
          break;
        }
        case TypeRequest.comando:
        {
          await Command.HandleCommand(solicitacao, user);
          break;
        }
        case TypeRequest.txtInfo:
          {
            var resposta_txt = ExecutarSap(
              solicitacao.application,
              solicitacao.information,
              solicitacao.instance
            );
            await bot.sendTextMesssageWraper(
              solicitacao.identifier,
              resposta_txt);
            bot.SucessReport(solicitacao);
            break;
          }
        case TypeRequest.picInfo:
          {
            var resposta_txt = ExecutarSap(
              solicitacao.application,
              solicitacao.information,
              solicitacao.instance
            );
            using(var image = ExecutarImg(resposta_txt))
            {
              await bot.SendPhotoAsyncWraper(solicitacao.identifier, image);
              logger.LogInformation("Enviado relatorio de {application}", solicitacao.application);
              bot.SucessReport(solicitacao);
              break;
            }
          }
        case TypeRequest.xlsInfo:
          {
            var resposta_txt = ExecutarSap(
              solicitacao.application,
              solicitacao.information,
              solicitacao.instance
            );
            if(solicitacao.identifier < 10)
            {
              HandleAnnouncement.Vencimento(resposta_txt, solicitacao);
              bot.SucessReport(solicitacao);
              break;
            }
            var bytearray = System.Text.Encoding.UTF8.GetBytes(resposta_txt);
            using(var memstream = new MemoryStream(bytearray))
            {
              var filename = new String[] {
                solicitacao.application,
                DateTime.Now.ToString("yyyyMMddHHmmss")
              };
              await bot.SendDocumentAsyncWraper(
                solicitacao.identifier,
                memstream,
                String.Join('_', filename) + ".csv"
              );
              logger.LogInformation("Enviado planilha de {application}", solicitacao.application);
              bot.SucessReport(solicitacao);
              break;
            }
          }
        case TypeRequest.xyzInfo:
          {
            var resposta_txt = ExecutarSap(
              solicitacao.application,
              solicitacao.information,
              solicitacao.instance
            );
            var re = new System.Text.RegularExpressions.Regex(@"-[0-9]{1,2}[\.|,][0-9]{5,}");
            var matches = re.Matches(resposta_txt);
            if(matches.Count != 2)
            {
              solicitacao.status = 500;
              var erro = new Exception(
                "A resposta recebida do SAP não é uma coordenada válida!");
              await bot.ErrorReport(erro, solicitacao);
              break;
            }
            var lat = Double.Parse(matches[0].Value.Replace('.', ','));
            var lon = Double.Parse(matches[1].Value.Replace('.', ','));
            await bot.SendCoordinateAsyncWraper(solicitacao.identifier, lat, lon);
            bot.SucessReport(solicitacao);
            break;
          }
        case TypeRequest.pdfInfo:
          {
            if(!cfg.GERAR_FATURAS)
            {
              solicitacao.status = 503;
              var erro = new Exception(
                "O sistema SAP não está gerando faturas no momento!");
              await bot.ErrorReport(erro, solicitacao);
              break;
            }
            var fluxo_atual = 0;
            var resposta_txt = ExecutarSap(
              "instalacao",
              solicitacao.information,
              solicitacao.instance
            );
            if(!Int64.TryParse(resposta_txt, out Int64 instalation))
            {
              solicitacao.status = 500;
              var erro = new Exception(
                "Não foi recebido o número da instalação!");
              await bot.ErrorReport(erro, solicitacao);
              break;
            }
            var agora = DateTime.Now;
            logger.LogDebug("Solicitando as faturas...");
            resposta_txt = ExecutarSap(
              solicitacao.application,
              instalation,
              solicitacao.instance
            );
            logger.LogDebug("Quantidade experada: {quantidade}", resposta_txt);
            if(!Int32.TryParse(resposta_txt, out Int32 quantidade_experada))
            {
              solicitacao.status = 500;
              var erro = new Exception(
                "Quantidade de faturas desconhecida!");
              await bot.ErrorReport(erro, solicitacao);
              break;
            }
            var faturas = new List<pdfsModel>();
            var tasks = new List<Task>();
            while (true)
            {
              await Task.Delay(cfg.TASK_DELAY_LONG);
              logger.LogDebug("Realizando a checagem");
              faturas = database.RecuperarFatura(
                f => f.instalation == instalation &&
                  f.status == pdfsModel.Status.wait
              );
              foreach (var fatura in faturas)
                logger.LogDebug(fatura.filename);
              if(faturas.Count == quantidade_experada) break;
              if(agora.AddMilliseconds(cfg.SAP_ESPERA) < DateTime.Now) break;
            }
            logger.LogDebug("Quantidade de faturas: ", faturas.Count);
            if(!faturas.Any())
            {
              solicitacao.status = 503;
              var erro = new Exception(
                "Não foi gerada nenhuma fatura pelo sistema SAP!");
              await bot.ErrorReport(erro, solicitacao);
              logger.LogError("Não foi gerada nenhuma fatura pelo sistema SAP!");
              break;
            }
            if(faturas.Count != quantidade_experada)
            {
              solicitacao.status = 503;
              var erro = new Exception(
                "A quantidade de faturas não condiz com a quantidade esperada!");
              await bot.ErrorReport(erro, solicitacao);
              logger.LogError("A quantidade de faturas não condiz com a quantidade esperada!");
              break;
            }
            var fluxos = new Stream[quantidade_experada];
            foreach (var fatura in faturas)
            {
              if(fatura.status == pdfsModel.Status.sent) continue;
              var caminho = System.IO.Path.Combine(cfg.TEMP_FOLDER, fatura.filename);
              fluxos[fluxo_atual] = System.IO.File.OpenRead(caminho);
              tasks.Add(bot.SendDocumentAsyncWraper(
                solicitacao.identifier,
                fluxos[fluxo_atual],
                fatura.filename
              ));
              fatura.status = pdfsModel.Status.sent;
              database.AlterarFatura(fatura);
              logger.LogInformation("Enviada fatura ({fluxo_atual}/{quantidade_experada}): {filename}",
              ++fluxo_atual, quantidade_experada, fatura.filename
              );
            }
            await Task.WhenAll(tasks);
            foreach(var fluxo in fluxos) fluxo.Close();
            PdfHandle.Remove(faturas);
            bot.SucessReport(solicitacao);
            logger.LogInformation("Enviadas faturas para a instalação {instalation}", instalation);
            break;
          }
        case TypeRequest.ofsInfo:
          {
            if(!user.pode_relatorios())
            {
              solicitacao.status = 403;
              var erro = new Exception(
                "Você não tem permissão para receber esse tipo de informação!");
              await bot.ErrorReport(erro, solicitacao);
              break;
            }
            if(!cfg.OFS_MONITORAMENTO)
            {
              solicitacao.status = 404;
              var erro = new Exception(
                "O sistema monitor do OFS não está ativo no momento!");
              await bot.ErrorReport(erro, solicitacao);
              break;
            }
            var agora = DateTime.Now;
            while (true)
            {
              await Task.Delay(cfg.TASK_DELAY_LONG);
              OfsHandle.Enrol(
                solicitacao.application,
                solicitacao.information,
                solicitacao.received_at
              );
              if(agora.AddMilliseconds(cfg.SAP_ESPERA) < DateTime.Now) break;
            }
            break;
          }
        default:
        {
          solicitacao.status = 400;
          var erro = new Exception(
            "Esse tipo de solicitação não está disponível no momento!");
          await bot.ErrorReport(erro, solicitacao);
          break;
        }
      }
      }
    catch (System.Exception erro)
    {
      await bot.ErrorReport(erro, solicitacao);
    }
  }
}