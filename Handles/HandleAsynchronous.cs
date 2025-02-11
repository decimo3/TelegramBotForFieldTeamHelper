using telbot.Helpers;
using telbot.models;
using telbot.Services;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Text.Json;
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
    try
    {
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
    if(
      request.typeRequest == TypeRequest.gestao ||
      request.typeRequest == TypeRequest.comando)
    {
      await Cooker(request);
    }
    }
    catch (System.Exception erro)
    {
      logger.LogError(erro, "Ocorreu um erro fatal!");
    }
  }
  public static async Task Chief
  (
    Int32 minInstance,
    Int32 maxInstance,
    Expression<Func<logsModel,bool>> filtro
  )
  {
    var instances = maxInstance - minInstance;
    var bot = HandleMessage.GetInstance();
    var cfg = Configuration.GetInstance();
    var database = Database.GetInstance();
    var logger = Logger.GetInstance<HandleAsynchronous>();
    var instanceControl = new bool[instances];
    var semaphore = new SemaphoreSlim(instances);
    while (true)
    {
      try
      {
      await Task.Delay(cfg.TASK_DELAY);
      var solicitacoes = database.RecuperarSolicitacao(filtro);
      var solicitacao_texto = JsonSerializer.Serialize(solicitacoes);
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
              instanceNumber = i + minInstance + 1;
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
                            instanceControl[instanceNumber - minInstance - 1] = false;
                          }
                          // Release the semaphore slot
                            semaphore.Release();
                        }
                    }));
      }
      await Task.WhenAll(tasks);
    }
    catch (System.Exception erro)
    {
      semaphore.Release(instances);
      logger.LogError(erro, "Ocorreu um erro fatal!");
    }
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
        if(cfg.SAP_OFFLINE)
        {
          solicitacao.status = 503;
          var erro = new Exception("O sistema SAP está fora do ar!");
          await bot.ErrorReport(erro, solicitacao);
          return;
        }
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
            // Testar se foi enviada o número de instalação
            if (solicitacao.information > 999999999 || solicitacao.information < 99999999)
            {
              solicitacao.status = 400;
              var erro = new Exception(
                "Só será aceita solicitação de fatura pela instalação!");
              await bot.ErrorReport(erro, solicitacao);
              break;
            }
            var resposta_txt = ExecutarSap(
              solicitacao.application,
              solicitacao.information,
              solicitacao.instance
            );
            if(!Int32.TryParse(resposta_txt, out Int32 quantidade_experada))
            {
              solicitacao.status = 500;
              var erro = new Exception(
                "Quantidade de faturas desconhecida!");
              await bot.ErrorReport(erro, solicitacao);
              break;
            }
            solicitacao.instance = quantidade_experada;
            solicitacao.response_at = DateTime.Now;
            solicitacao.status = 300;
            database.AlterarSolicitacao(solicitacao);
            await bot.sendTextMesssageWraper(solicitacao.identifier,
              "Sua fatura foi solicitada, favor aguardar a geração!"
              );
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