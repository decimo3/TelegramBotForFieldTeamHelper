using telbot.Services;
using telbot.models;
using Microsoft.Extensions.Logging;
namespace telbot.Helpers;
public partial class PdfHandle
{
  public static async void Watch()
  {
    var cfg = Configuration.GetInstance();
    var database = Database.GetInstance();
    var logger = Logger.GetInstance<PdfHandle>();
    logger.LogDebug("Monitor de faturas iniciado!");
    logger.LogDebug(cfg.TEMP_FOLDER);
    while (true)
    {
      try
      {
        await Task.Delay(cfg.TASK_DELAY);
        logger.LogDebug("Realizando o escaneamento de faturas...");
        var files = System.IO.Directory.GetFiles(cfg.TEMP_FOLDER);
        foreach (var file in files)
        {
          if(System.IO.Path.GetExtension(file) != ".pdf") continue;
          var filename = System.IO.Path.GetFileName(file);
          var registry = database.RecuperarFatura(filename);
          if(registry == null)
          {
            var instalation = PdfHandle.Check(file);
            var timestamp = System.IO.File.GetLastWriteTime(file);
            var fatura = new pdfsModel() {
              filename = filename,
              instalation = instalation,
              timestamp = timestamp,
              status = pdfsModel.Status.wait
            };
            var fatura_txt = System.Text.Json.JsonSerializer.Serialize<pdfsModel>(fatura);
            logger.LogDebug(fatura_txt);
            database.InserirFatura(fatura);
          }
          else
          {
            if(registry.status == pdfsModel.Status.sent)
            {
              System.IO.File.Delete(filename);
              registry.status = pdfsModel.Status.done;
              database.AlterarFatura(registry);
            }
          }
        }
      }
      catch (System.Exception erro)
      {
        logger.LogError(erro, "Ocorreu uma falha ao monitorar o diretório de faturas: ");
      }
    }
  }
}