using Microsoft.Extensions.Logging;
using telbot.models;
namespace telbot.Helpers
{
  public partial class PdfHandle
  {
    public async void Sender()
    {
      logger.LogDebug("Realizando a procura de faturas para entregar...");
      while (true)
      {
        try
        {
          await Task.Delay(cfg.TASK_DELAY);
          // Recupera as solicitações redirecionadas
          var solicitacoes = database.RecuperarSolicitacao(
            r => r.status == 300 &&
            r.typeRequest == models.TypeRequest.pdfInfo);
          // Verifica se há solicitações a serem enviadas
          if (solicitacoes is null)
          {
            continue;
          }
          var tasks = new List<Task>();
          // Verifica em cada solicitação, se já foi gerada a fatura
          foreach (var solicitacao in solicitacoes)
          {
            List<pdfsModel>? faturas_info = null;
            // Recupera a lista de faturas geradas
            faturas_info = new List<pdfsModel>(
              faturas.Where(f =>
              f.timestamp >= solicitacao.received_at &&
              f.instalation == solicitacao.information
            ).ToList());
            // Verifica se já tem faturas para a solicitação e se é a quantidade esperada
            if (faturas_info is not null && faturas_info.Count != solicitacao.instance)
            {
              //! Se tudo der certo, aqui tem que começar a entregar as faturas
              tasks.Add(Sender(faturas_info, solicitacao));
            }
            // Verifica se a solicitação já não expirou
            if(solicitacao.response_at.AddMilliseconds(cfg.SAP_ESPERA) < DateTime.Now)
            {
              solicitacao.status = 503;
              tasks.Add(bot.ErrorReport(
                request: solicitacao,
                error: new Exception("Não foi gerada nenhuma fatura pelo sistema SAP!")
              ));
            }
          }
          await Task.WhenAll(tasks);
        }
        catch (System.Exception erro)
        {
          logger.LogError(erro, "Ocorreu um erro ao tentar enviar a fatura!");
        }
      }
    }
    // Método separado para enviar as faturas
    private async Task Sender(List<pdfsModel> faturasInfo, logsModel solicitacao)
    {
      var fluxoAtual = 0;
      var tasks = new List<Task>();
      var fluxos = new Stream[solicitacao.instance];
      foreach (var fatura in faturasInfo)
      {
        if (fatura.status == pdfsModel.Status.sent) continue;
        var caminho = System.IO.Path.Combine(cfg.TEMP_FOLDER, fatura.filename);
        fluxos[fluxoAtual] = System.IO.File.OpenRead(caminho);
        tasks.Add(bot.SendDocumentAsyncWraper(
          solicitacao.identifier,
          fluxos[fluxoAtual],
          fatura.filename
        ));
        fatura.status = pdfsModel.Status.sent;
        logger.LogInformation("Enviada fatura ({fluxoAtual}/{quantidadeEsperada}): {filename}",
          ++fluxoAtual, solicitacao.instance, fatura.filename);
      }
      await Task.WhenAll(tasks);
      foreach (var fluxo in fluxos)
      {
        fluxo.Close();
      }
      Remove(faturasInfo);
      bot.SucessReport(solicitacao);
      logger.LogInformation("Enviadas faturas para a instalação {instalation}", solicitacao.information);
    }
  }
}
