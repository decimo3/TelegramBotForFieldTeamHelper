using telbot.API;
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
      await telegram.sendTextMesssageWraper(identificador, "Verifique o formato da informação e tente novamente da forma correta!");
      await telegram.sendTextMesssageWraper(identificador, "Se tiver em dúvida de como usar o bot, digite /ajuda.");
      return;
    }
    request.identifier = identificador;
    request.received_at = received_at.ToLocalTime();
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
      ConsoleWrapper.Debug(Entidade.CookerAsync, $"Instância {instance} buscando solicitações...");
      var solicitacao = database.RecuperarSolicitacao(
        s => s.status == 0 && (s.rowid - instance) % cfg.SAP_INSTANCIA == 0
      ).FirstOrDefault();
      if (solicitacao == null)
      {
        ConsoleWrapper.Debug(Entidade.CookerAsync, $"Instância {instance} não encontrou solicitações!");
        continue;
      }
      var solicitacao_texto = System.Text.Json.JsonSerializer.Serialize<logsModel>(solicitacao);
      ConsoleWrapper.Debug(Entidade.CookerAsync, solicitacao_texto);
      if(solicitacao.received_at.AddSeconds(cfg.ESPERA) < DateTime.Now)
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
            instance.ToString()
          };
          var resposta_txt = Executor.Executar("sap.exe", argumentos, true);
          var resposta = System.Text.Json.JsonSerializer.Deserialize<Response>(resposta_txt);
          if(resposta == null)
          {
            var erro = new IndexOutOfRangeException("Não foi recebida nenhuma resposta do `SAP_BOT`!");
            await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
            break;
          }
          if(resposta.status == 200)
          {
            await bot.sendTextMesssageWraper(solicitacao.identifier, String.Join('\n', resposta.data));
            bot.SucessReport(solicitacao);
          }
          else
          {
            solicitacao.status = resposta.status;
            var erro = new Exception(resposta.data);
            await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
          }
          break;
        }
        case TypeRequest.picInfo:
        {
          var argumentos = new String[] {
            solicitacao.application,
            solicitacao.information.ToString(),
            instance.ToString()
          };
          var resposta_txt = Executor.Executar("sap.exe", argumentos, true);
          var resposta = System.Text.Json.JsonSerializer.Deserialize<Response>(resposta_txt);
          if(resposta == null)
          {
            var erro = new IndexOutOfRangeException("Não foi recebida nenhuma resposta do `SAP_BOT`!");
            await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
            break;
          }
          if(resposta.status == 200)
          {
            argumentos = new String[] { resposta.data };
            resposta_txt = Executor.Executar("img.exe", argumentos, true);
            resposta = System.Text.Json.JsonSerializer.Deserialize<Response>(resposta_txt);
            if(resposta == null)
            {
              var erro = new IndexOutOfRangeException("Não foi recebida nenhuma resposta do `IMG2CSV`!");
              await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
              break;
            }
            if(resposta.status == 200)
            {
              var bytearray = Convert.FromBase64String(resposta.data);
              using(var memstream = new MemoryStream(bytearray))
              {
                await bot.SendPhotoAsyncWraper(solicitacao.identifier, memstream);
                bot.SucessReport(solicitacao);
                break;
              }
            }
            else
            {
              solicitacao.status = resposta.status;
              var erro = new Exception(resposta.data);
              await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
              break;
            }
          }
          else
          {
            solicitacao.status = resposta.status;
            var erro = new Exception(resposta.data);
            await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
            break;
          }
        }
        case TypeRequest.xlsInfo:
        {
          var argumentos = new String[] {
            solicitacao.application,
            solicitacao.information.ToString(),
            instance.ToString()
          };
          var resposta_txt = Executor.Executar("sap.exe", argumentos, true);
          var resposta = System.Text.Json.JsonSerializer.Deserialize<Response>(resposta_txt);
          if(resposta == null)
          {
            var erro = new IndexOutOfRangeException("Não foi recebida nenhuma resposta do `SAP_BOT`!");
            await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
            break;
          }
          if(resposta.status == 200)
          {
            var bytearray = System.Text.Encoding.ASCII.GetBytes(resposta.data);
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
          break;
        }
        case TypeRequest.xyzInfo:
        {
          var argumentos = new String[] {
            solicitacao.application,
            solicitacao.information.ToString(),
            instance.ToString()
          };
          var resposta_txt = Executor.Executar("sap.exe", argumentos, true);
          var resposta = System.Text.Json.JsonSerializer.Deserialize<Response>(resposta_txt);
          if(resposta == null)
          {
            var erro = new IndexOutOfRangeException("Não foi recebida nenhuma resposta do `SAP_BOT`!");
            await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
            break;
          }
          if(resposta.status == 200)
          {
            await bot.SendCoordinateAsyncWraper(solicitacao.identifier, String.Join('\n', resposta.data));
            bot.SucessReport(solicitacao);
          }
          else
          {
            solicitacao.status = resposta.status;
            var erro = new Exception(resposta.data);
            await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
          }
          break;
        }
        case TypeRequest.pdfInfo:
        {
          var argumentos = new String[] {
            solicitacao.application,
            solicitacao.information.ToString(),
            instance.ToString()
          };
          var resposta_txt = Executor.Executar("sap.exe", argumentos, true);
          var resposta = System.Text.Json.JsonSerializer.Deserialize<Response>(resposta_txt);
          if(resposta == null)
          {
            var erro = new IndexOutOfRangeException("Não foi recebida nenhuma resposta do `SAP_BOT`!");
            await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
            break;
          }
          if(resposta.status == 200)
          {
            var fluxo_atual = 0;
            var agora = DateTime.Now;
            var quantidade_experada = Int32.Parse(resposta.data);
            var fluxos = new Stream[quantidade_experada];
            var faturas = new List<pdfsModel>();
            // TODO - Adicionar tempo de expiração para a solicitação
            while (true)
            {
              await Task.Delay(cfg.TASK_DELAY_LONG);
              faturas = database.RecuperarFatura(
                f => f.instalation == solicitacao.information &&
                !f.has_expired()
              );
              if(faturas.Count == quantidade_experada) break;
              if((DateTime.Now - agora).Seconds > cfg.ESPERA) break;
            }
            if(faturas.Count != quantidade_experada)
            {
              var erro = new Exception("A quantidade de faturas impressas não está batendo com a quantidade esperada!");
              await bot.ErrorReport(solicitacao.information, erro, solicitacao);
              continue;
            }
            var tasks = new List<Task>();
            
            foreach (var fatura in faturas)
            {
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
          else
          {
            solicitacao.status = resposta.status;
            var erro = new Exception(resposta.data);
            await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
          }
          break;
        }
        case TypeRequest.ofsInfo:
        {
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