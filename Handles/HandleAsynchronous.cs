using telbot.API;
using telbot.Helpers;
using telbot.models;
using telbot.Services;
namespace telbot.handle;
public static class HandleAsynchronous
{
  // DONE - Recebe, verifica e registra no banco de dados
  public static async Task Soiree(Int64 identificador, String mensagem, DateTime received_at)
  {
    var database = Database.GetInstance();
    var telegram = HandleMessage.GetInstance();
    ConsoleWrapper.Write(Entidade.Usuario, $"{identificador} escreveu: {mensagem}");
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
        case TypeRequest.pdfInfo:
        case TypeRequest.picInfo:
        case TypeRequest.xlsInfo:
        case TypeRequest.xyzInfo:
        {
          var argumentos = new String[] {
            solicitacao.application,
            solicitacao.information.ToString(),
            "--instancia=" + instance,
            "--timestamp=" + solicitacao.received_at.ToLocalTime().ToString("yyyyMMddHHmmss")
          };
          Executor.Executar("sap.exe", argumentos, false);
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
      solicitacao = database.RecuperarSolicitacao(
        s => s.rowid == solicitacao.rowid
      ).Single();
      if(solicitacao.status == 0)
      {
        solicitacao.instance = instance;
        solicitacao.status = 300;
        database.AlterarSolicitacao(solicitacao);
      }
    }
  }
  // DONE - Coleta resposta e responde ao usuário
  public static async void Waiter(Int32 instance)
  {
    ConsoleWrapper.Debug(Entidade.WaiterAsync, $"Instância {instance} iniciada!");
    var cfg = Configuration.GetInstance();
    var database = Database.GetInstance();
    var bot = HandleMessage.GetInstance();
    while (true)
    {
      await Task.Delay(cfg.TASK_DELAY);
      ConsoleWrapper.Debug(Entidade.WaiterAsync, $"Instância {instance} buscando respostas...");
      var solicitacao = database.RecuperarSolicitacao(
        s => s.status == 300 && (s.rowid - instance) % cfg.SAP_INSTANCIA == 0
      ).FirstOrDefault();
      if (solicitacao == null)
      {
        ConsoleWrapper.Debug(Entidade.WaiterAsync, $"Instância {instance} não encontrou respostas!");
        continue;
      }
      var solicitacao_texto = System.Text.Json.JsonSerializer.Serialize<logsModel>(solicitacao);
      ConsoleWrapper.Debug(Entidade.WaiterAsync, solicitacao_texto);
      if(solicitacao.received_at.AddSeconds(cfg.ESPERA) < DateTime.Now)
      {
        var erro = new Exception("A sua solicitação expirou!");
        await bot.ErrorReport(solicitacao.identifier, erro, solicitacao);
        return;
      }
      var arguments = new String[] {
        solicitacao.received_at.ToLocalTime().ToString("yyyyMMddHHmmss"),
        solicitacao.application,
        solicitacao.information.ToString(),
        ".json"
      };
      var filename = String.Join('_', arguments);
      if(!System.IO.File.Exists(filename)) continue;
      var response = System.Text.Json.JsonSerializer.Deserialize<Response>(filename);
      if(response == null) continue;
      var tasks = new List<Task>();
      var count = response.entities.Where(
        e => e.type == typeEntity.PIC || e.type == typeEntity.XLS
      ).Count();
      var expected_invoices = response.entities.Where(
        e => e.type == typeEntity.PDF
      ).Sum(e => Int32.Parse(e.data));
      var fluxos = new Stream[count + expected_invoices];
      var fluxo_atual = 0;
      var faturas = new List<pdfsModel>();
      for(var i = 0; i < response.entities.Count; i++)
      {
        switch (response.entities[i].type)
        {
          case typeEntity.TXT:
          {
            tasks.Add(bot.sendTextMesssageWraper(
              solicitacao.identifier,
              response.entities[i].data
            ));
            break;
          }
          case typeEntity.XYZ:
          {
            tasks.Add(bot.SendCoordinateAsyncWraper(
              solicitacao.identifier,
              response.entities[i].data
            ));
            break;
          }
          case typeEntity.PIC:
          {
            var bytearray = Convert.FromBase64String(response.entities[i].data);
            fluxos[fluxo_atual] = new MemoryStream(bytearray);
            tasks.Add(bot.SendPhotoAsyncWraper(
              solicitacao.identifier,
              fluxos[fluxo_atual]
            ));
            fluxo_atual++;
            break;
          }
          case typeEntity.XLS:
          {
            var bytearray = System.Text.Encoding.UTF8.GetBytes(response.entities[i].data);
            fluxos[fluxo_atual] = new MemoryStream(bytearray);
            var nomeenvio = new String[] {
              solicitacao.application,
              "_",
              solicitacao.received_at.ToLocalTime().ToString("U"),
              ".csv"
            };
            tasks.Add(bot.SendDocumentAsyncWraper(
              solicitacao.identifier,
              fluxos[fluxo_atual],
              String.Join("", nomeenvio)
            ));
            fluxo_atual++;
            break;
          }
          case typeEntity.PDF:
          {
            var agora = DateTime.Now;
            while(true)
            {
              faturas = database.RecuperarFatura(
                f => !f.has_expired() &&
                f.instalation == solicitacao.information
              );
              if(faturas.Count == expected_invoices) break;
              if((DateTime.Now - agora).Seconds > cfg.ESPERA) break;
              faturas = new List<pdfsModel>();
              await Task.Delay(cfg.TASK_DELAY);
            }
            if(faturas.Count != expected_invoices)
            {
              var erro = new Exception("A quantidade de faturas impressas não está batendo com a quantidade esperada!");
              tasks.Add(bot.ErrorReport(solicitacao.information, erro, solicitacao));
              continue;
            }
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
            break;
          }
        }
      }
      await Task.WhenAll(tasks);
      foreach(var fluxo in fluxos) fluxo.Close();
      solicitacao.status = response.status;
      if(solicitacao.status == 200)
      {
        bot.SucessReport(solicitacao);
      }
      else
      {
        await bot.ErrorReport(
          solicitacao.identifier,
          new Exception(),
          solicitacao
        );
      }
    }
  }
}