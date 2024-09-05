using telbot.Helpers;
using telbot.models;
using telbot.Services;
namespace telbot.handle;
public static class HandleAsynchronous
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
      (instance - 1).ToString()
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
  // DONE - Recebe, verifica e registra no banco de dados
  public static async Task Waiter(Int64 identificador, String mensagem, DateTime received_at)
  {
    var database = Database.GetInstance();
    var telegram = HandleMessage.GetInstance();
    ConsoleWrapper.Write(Entidade.WaiterAsync, $"{identificador} escreveu: {mensagem}");
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
  // DONE - Coleta do banco de dados e realiza a solicitação
  public static async void Cooker(Int32 instance)
  {
    ConsoleWrapper.Debug(Entidade.CookerAsync, $"Instância {instance} iniciada!");
    var bot = HandleMessage.GetInstance();
    var cfg = Configuration.GetInstance();
    var database = Database.GetInstance();
    while (true)
    {
      await Task.Delay(cfg.TASK_DELAY);
      var solicitacao = database.RecuperarSolicitacao(
        s => s.status == 0 && (s.rowid - instance) % cfg.SAP_INSTANCIA == 0
      ).FirstOrDefault();
      if (solicitacao == null)
      {
        continue;
      }
      solicitacao.instance = instance;
      var solicitacao_texto = System.Text.Json.JsonSerializer.Serialize<logsModel>(solicitacao);
      ConsoleWrapper.Debug(Entidade.CookerAsync, solicitacao_texto);
      if(solicitacao.received_at.AddMilliseconds(cfg.SAP_ESPERA) < DateTime.Now)
      {
        solicitacao.status = 408;
        var erro = new Exception("A sua solicitação expirou!");
        await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
        continue;
      }
      switch (solicitacao.typeRequest)
      {
        case TypeRequest.gestao:
        {
          await Manager.HandleManager(solicitacao);
          break;
        }
        case TypeRequest.comando:
        {
          await Command.HandleCommand(solicitacao);
          break;
        }
        case TypeRequest.txtInfo:
          try
          {
            var resposta_txt = ExecutarSap(
              solicitacao.application,
              solicitacao.information,
              instance
            );
            await bot.sendTextMesssageWraper(
              solicitacao.identifier,
              resposta_txt);
            bot.SucessReport(solicitacao);
            break;
          }
          catch (System.Exception erro)
          {
            var match = regex.Match(erro.Message);
            var texto = new Exception(erro.Message[5..]);
            solicitacao.status = Int32.Parse(match.Value);
            await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
            break;
          }
        case TypeRequest.picInfo:
          try
          {
            var resposta_txt = ExecutarSap(
              solicitacao.application,
              solicitacao.information,
              instance
            );
            using(var image = ExecutarImg(resposta_txt))
            {
              await bot.SendPhotoAsyncWraper(solicitacao.identifier, image);
              bot.SucessReport(solicitacao);
              break;
            }
          }
          catch (System.Exception erro)
          {
            var match = regex.Match(erro.Message);
            var texto = new Exception(erro.Message[5..]);
            solicitacao.status = Int32.Parse(match.Value);
            await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
            break;
          }
        case TypeRequest.xlsInfo:
          try
          {
            var resposta_txt = ExecutarSap(
              solicitacao.application,
              solicitacao.information,
              instance
            );
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
              bot.SucessReport(solicitacao);
              break;
            }
          }
          catch (System.Exception erro)
          {
            var match = regex.Match(erro.Message);
            var texto = new Exception(erro.Message[5..]);
            solicitacao.status = Int32.Parse(match.Value);
            await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
            break;
          }
        case TypeRequest.xyzInfo:
          try
          {
            var resposta_txt = ExecutarSap(
              solicitacao.application,
              solicitacao.information,
              instance
            );
            await bot.SendCoordinateAsyncWraper(solicitacao.identifier, resposta_txt);
            bot.SucessReport(solicitacao);
            break;
          }
          catch (System.Exception erro)
          {
            var match = regex.Match(erro.Message);
            var texto = new Exception(erro.Message[5..]);
            solicitacao.status = Int32.Parse(match.Value);
            await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
            break;
          }
        case TypeRequest.pdfInfo:
          try
          {
            var fluxo_atual = 0;
            var agora = DateTime.Now;
            var resposta_txt = ExecutarSap(
              "instalacao",
              solicitacao.information,
              instance
            );
            if(!Int64.TryParse(resposta_txt, out Int64 instalation))
            {
              throw new InvalidOperationException(
                "500: Não foi recebido o número da instalação!");
            }
            resposta_txt = ExecutarSap(
              solicitacao.application,
              instalation,
              instance
            );
            if(!Int32.TryParse(resposta_txt, out Int32 quantidade_experada))
            {
              throw new InvalidOperationException(
                "500: Quantidade de faturas desconhecida!");
            }
            var faturas = new List<pdfsModel>();
            var tasks = new List<Task>();
            while (true)
            {
              await Task.Delay(cfg.TASK_DELAY_LONG);
              faturas = database.RecuperarFatura(
                f => f.instalation == instalation &&
                !f.has_expired() && f.status == pdfsModel.Status.wait
              );
              if(faturas.Count == quantidade_experada) break;
              if((DateTime.Now - agora) > TimeSpan.FromMilliseconds(cfg.SAP_ESPERA)) break;
            }
            if(!faturas.Any())
            {
              solicitacao.status = 503;
              var erro = new Exception(
                "Não foi gerada nenhuma fatura pelo sistema SAP!");
              await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
              break;
            }
            if(faturas.Count != quantidade_experada)
            {
              solicitacao.status = 503;
              var erro = new Exception(
                "A quantidade de faturas não condiz com a quantidade esperada!");
              await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
              break;
            }
            var fluxos = new Stream[quantidade_experada];
            foreach (var fatura in faturas)
            {
              if(fatura.status == pdfsModel.Status.sent) continue;
              fluxos[fluxo_atual] = System.IO.File.OpenRead(fatura.filename);
              tasks.Add(bot.SendDocumentAsyncWraper(
                solicitacao.identifier,
                fluxos[fluxo_atual],
                fatura.filename
              ));
              fatura.status = pdfsModel.Status.sent;
              database.AlterarFatura(fatura);
              fluxo_atual++;
            }
            await Task.WhenAll(tasks);
            foreach(var fluxo in fluxos) fluxo.Close();
            bot.SucessReport(solicitacao);
            break;
          }
          catch (System.Exception erro)
          {
            var match = regex.Match(erro.Message);
            var texto = new Exception(erro.Message[5..]);
            solicitacao.status = Int32.Parse(match.Value);
            await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
            break;
          }
        case TypeRequest.ofsInfo:
          try
          {
            var agora = DateTime.Now;
            OfsHandle.Enrol(
              solicitacao.application,
              solicitacao.information,
              solicitacao.received_at
            );
            break;
          }
          catch (System.Exception erro)
          {
            var match = regex.Match(erro.Message);
            var texto = new Exception(erro.Message[5..]);
            solicitacao.status = Int32.Parse(match.Value);
            await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
            break;
          }
      }
    }
  }
}