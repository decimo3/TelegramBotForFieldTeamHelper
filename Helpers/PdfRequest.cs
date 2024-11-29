using telbot.Services;
using telbot.models;
using Microsoft.Extensions.Logging;
using telbot.handle;
namespace telbot.Helpers;
public partial class PdfHandle
{
  public static async void Request()
  {
    var bot = HandleMessage.GetInstance();
    var cfg = Configuration.GetInstance();
    var database = Database.GetInstance();
    var logger = Logger.GetInstance<PdfHandle>();
    logger.LogDebug("Solicitador de faturas iniciado!");
    while (true)
    {
      await Task.Delay(cfg.TASK_DELAY);
      var solicitacoes = database.RecuperarSolicitacao(
        s => s.typeRequest == TypeRequest.pdfInfo &&
        s.status == 0);
      foreach (var solicitacao in solicitacoes)
      {
        try
        {
          if(HandleAnnouncement.Executador("prl.exe") != 1)
          {
            throw new Exception("503: Subsistema `PRL_BOT` não está em execução no momento!");
          }
          if(solicitacao.information > 999999999 || solicitacao.information < 99999999)
          {
            throw new Exception("400: Subsistema `PRL_BOT` só trabalha com a instalação!");
          }
          HandlerLockfile.EscreverLockFile(
            "prl.lock",
            solicitacao.application,
            solicitacao.information
            );
          var antes = DateTime.Now;
          var result = String.Empty;
          while (true)
          {
            await Task.Delay(cfg.TASK_DELAY_LONG);
            result = HandlerLockfile.VerificarLockfile("prl.lock");
            if(!String.IsNullOrEmpty(result)) break;
            if((DateTime.Now - antes) >= TimeSpan.FromSeconds(cfg.SAP_ESPERA)) break;
          }
          if(String.IsNullOrEmpty(result))
          {
            throw new Exception("408: Não foi recebida nenhuma resposta do `PRL_BOT`!");
          }
          if(HandleAsynchronous.regex.IsMatch(result))
          {
            throw new Exception(result);
          }
          var resposta_obj = System.Text.Json.JsonSerializer.Deserialize<List<Fatura>>(result) ??
            throw new InvalidOperationException("503: A resposta recebida do PRL_BOT é inválida!");
          var fluxo_atual = 0;
          var tasks = new List<Task>();
          var faturas = new List<pdfsModel>();
          var quantidade_experada = resposta_obj.Count;
          while(true)
          {
            await Task.Delay(cfg.TASK_DELAY_LONG);
            logger.LogDebug("Realizando a checagem");
            faturas = database.RecuperarFatura(
              f => f.instalation == solicitacao.information &&
              f.timestamp >= antes
            );
            if(faturas.Count == quantidade_experada) break;
            if(antes.AddMilliseconds(cfg.SAP_ESPERA) < DateTime.Now) break;
          }
          logger.LogDebug("Quantidade de faturas: {faturas.Count}", faturas.Count);
          if(!faturas.Any())
          {
            throw new Exception("408: Não foi gerada nenhuma fatura pelo sistema `PRL_BOT`!");
          }
          if(faturas.Count != quantidade_experada)
          {
            throw new Exception("500: A quantidade de faturas não condiz com o esperado!");
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
          logger.LogInformation("Enviadas faturas para a instalação {instalation}", solicitacao.information);
        }
        catch (System.Exception erro)
        {
          await bot.ErrorReport(erro, solicitacao);
        }
      } 
    }
  }
}