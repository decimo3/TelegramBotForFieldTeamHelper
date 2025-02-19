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
          // Verifica em cada solicitação, se já foi gerada a fatura
          foreach (var solicitacao in solicitacoes)
          {
            // Verifica se a solicitação já não expirou
            if(!cfg.IS_DEVELOPMENT &&
              solicitacao.response_at.AddMilliseconds(cfg.SAP_ESPERA) < DateTime.Now)
            {
              solicitacao.status = 503;
              var erro = new Exception("Não foi gerada nenhuma fatura pelo sistema SAP!");
              await bot.ErrorReport(erro, solicitacao);
              continue;
            }
            List<pdfsModel>? faturas_info = null;
            // Recupera a lista de faturas geradas
            lock(_lock)
            {
              faturas_info = new List<pdfsModel>(
                faturas.Where(f =>
                f.timestamp >= solicitacao.received_at &&
                f.instalation == solicitacao.information
              ).ToList());
            }
            // Verifica se já tem faturas para a solicitação
            if (faturas_info is null)
            {
              continue;
            }
            // Verifica se a quantidade de faturas encontradas é a mesma quantidade esperada 
            if (faturas_info.Count != solicitacao.instance)
            {
              continue;
            }
            //! Se tudo der certo, aqui tem que começar a entregar as faturas
            var fluxo_atual = 0;
            var tasks = new List<Task>();
            var fluxos = new Stream[solicitacao.instance];
            foreach (var fatura in faturas_info)
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
              logger.LogInformation("Enviada fatura ({fluxo_atual}/{quantidade_experada}): {filename}",
                ++fluxo_atual, solicitacao.instance, fatura.filename);
            }
            await Task.WhenAll(tasks);
            foreach(var fluxo in fluxos) fluxo.Close();
            lock(_lock)
            {
              Remove(faturas_info);
            }
            bot.SucessReport(solicitacao);
            logger.LogInformation("Enviadas faturas para a instalação {instalation}", solicitacao.information);
          }
        }
        catch (System.Exception erro)
        {
          logger.LogError(erro, "Ocorreu um erro ao tentar enviar a fatura!");
        }
      }
    }
  }
}