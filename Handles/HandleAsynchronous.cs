using telbot.Helpers;
using telbot.models;
using telbot.Services;
namespace telbot.handle;
public static class HandleAsynchronous
{
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
    var regex = new System.Text.RegularExpressions.Regex("^[0-9]{3}");
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
        var erro = new Exception("A sua solicitação expirou!");
        await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
        return;
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
        {
          var argumentos = new String[] {
            solicitacao.application,
            solicitacao.information.ToString(),
            (instance - 1).ToString()
          };
          var resposta_txt = Executor.Executar("sap.exe", argumentos, true);
          if(String.IsNullOrEmpty(resposta_txt))
          {
            var erro = new IndexOutOfRangeException(
              "Não foi recebida nenhuma resposta do `SAP_BOT`!");
            await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
            break;
          }
          var match = regex.Match(resposta_txt);
          if(match.Success)
          {
            solicitacao.status = Int32.Parse(match.Value);
            var erro = new Exception(resposta_txt.Skip(5).ToString());
            await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
            break;
          }
          else
          {
            await bot.sendTextMesssageWraper(
              solicitacao.identifier,
              resposta_txt);
            bot.SucessReport(solicitacao);
            break;
          }
        }
        case TypeRequest.picInfo:
        {
          var argumentos = new String[] {
            solicitacao.application,
            solicitacao.information.ToString(),
            (instance - 1).ToString()
          };
          var resposta_txt = Executor.Executar("sap.exe", argumentos, true);
          if(String.IsNullOrEmpty(resposta_txt))
          {
            var erro = new IndexOutOfRangeException(
              "Não foi recebida nenhuma resposta do `SAP_BOT`!");
            await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
            break;
          }
          var match = regex.Match(resposta_txt);
          if(match.Success)
          {
            solicitacao.status = Int32.Parse(match.Value);
            var erro = new Exception(resposta_txt.Skip(5).ToString());
            await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
            break;
          }
          else
          {
            argumentos = new String[] { "\"" + resposta_txt + "\"" };
            resposta_txt = Executor.Executar("img.exe", argumentos, true);
            if(String.IsNullOrEmpty(resposta_txt))
            {
              var erro = new IndexOutOfRangeException(
                "Não foi recebida nenhuma resposta do `IMG2CSV`!");
              await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
              break;
            }
            match = regex.Match(resposta_txt);
            if(match.Success)
            {
              solicitacao.status = Int32.Parse(match.Value);
              var erro = new Exception(resposta_txt.Skip(5).ToString());
              await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
              break;
            }
            else
            {
              var bytearray = Convert.FromBase64String(resposta_txt);
              using(var memstream = new MemoryStream(bytearray))
              {
                await bot.SendPhotoAsyncWraper(solicitacao.identifier, memstream);
                bot.SucessReport(solicitacao);
                break;
              }
            }
          }
        }
        case TypeRequest.xlsInfo:
        {
          var argumentos = new String[] {
            solicitacao.application,
            solicitacao.information.ToString(),
            (instance - 1).ToString()
          };
          var resposta_txt = Executor.Executar("sap.exe", argumentos, true);
          if(String.IsNullOrEmpty(resposta_txt))
          {
            var erro = new IndexOutOfRangeException(
              "Não foi recebida nenhuma resposta do `SAP_BOT`!");
            await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
            break;
          }
          var match = regex.Match(resposta_txt);
          if(match.Success)
          {
            solicitacao.status = Int32.Parse(match.Value);
            var erro = new Exception(resposta_txt.Skip(5).ToString());
            await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
            break;
          }
          else
          {
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
        }
        case TypeRequest.xyzInfo:
        {
          var argumentos = new String[] {
            solicitacao.application,
            solicitacao.information.ToString(),
            (instance - 1).ToString()
          };
          var resposta_txt = Executor.Executar("sap.exe", argumentos, true);
          if(String.IsNullOrEmpty(resposta_txt))
          {
            var erro = new IndexOutOfRangeException(
              "Não foi recebida nenhuma resposta do `SAP_BOT`!");
            await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
            break;
          }
          var match = regex.Match(resposta_txt);
          if(match.Success)
          {
            solicitacao.status = Int32.Parse(match.Value);
            var erro = new Exception(resposta_txt.Skip(5).ToString());
            await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
            break;
          }
          else
          {
            await bot.SendCoordinateAsyncWraper(solicitacao.identifier, resposta_txt);
            bot.SucessReport(solicitacao);
          }
          break;
        }
        case TypeRequest.pdfInfo:
        {
          var argumentos = new String[] {
            solicitacao.application,
            solicitacao.information.ToString(),
            (instance - 1).ToString()
          };
          var resposta_txt = Executor.Executar("sap.exe", argumentos, true);
          if(String.IsNullOrEmpty(resposta_txt))
          {
            var erro = new IndexOutOfRangeException(
              "Não foi recebida nenhuma resposta do `SAP_BOT`!");
            await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
            break;
          }
          var match = regex.Match(resposta_txt);
          if(match.Success)
          {
            solicitacao.status = Int32.Parse(match.Value);
            var erro = new Exception(resposta_txt.Skip(5).ToString());
            await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
            break;
          }
          else
          {
            var fluxo_atual = 0;
            var agora = DateTime.Now;
            var quantidade_experada = Int32.Parse(resposta_txt);
            var fluxos = new Stream[quantidade_experada];
            var faturas = new List<pdfsModel>();
            var tasks = new List<Task>();
            while (true)
            {
              await Task.Delay(cfg.TASK_DELAY_LONG);
              faturas = database.RecuperarFatura(
                f => f.instalation == solicitacao.information &&
                !f.has_expired() && f.status == pdfsModel.Status.wait
              );
              if(faturas.Count == quantidade_experada) break;
              if((DateTime.Now - agora) > TimeSpan.FromMilliseconds(cfg.SAP_ESPERA)) break;
            }
            if(faturas.Count != quantidade_experada)
            {
              var erro = new Exception(
                "A quantidade de faturas não condiz com a quantidade esperada!");
              await bot.ErrorReport(solicitacao.information, erro, solicitacao);
              break;
            }
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
        }
        case TypeRequest.ofsInfo:
        {
          var agora = DateTime.Now;
          OfsHandle.Enrol(
            solicitacao.application,
            solicitacao.information,
            solicitacao.received_at
          );
          break;
        }
      }
    }
  }
}