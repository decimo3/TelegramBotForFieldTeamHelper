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
            if(instalation == 0)
            {
              System.IO.File.Delete(file);
              continue;
            }
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
        }
      }
      catch (System.Exception erro)
      {
        logger.LogError(erro, "Ocorreu uma falha ao monitorar o diretório de faturas: ");
      }
    }
  }
  public static void Remove(List<pdfsModel> faturas)
  {
    var logger = Logger.GetInstance<PdfHandle>();
    try
    {
      foreach (var fatura in faturas)
      {
        var filepath = System.IO.Path.Combine(
          Configuration.GetInstance().TEMP_FOLDER,
          fatura.filename
        );
        System.IO.File.Delete(filepath);
        Database.GetInstance().RemoverFatura(fatura.rowid);
        logger.LogDebug("Excluída fatura {fatura}", fatura.filename);
      }
    }
    catch (System.Exception erro)
    {
      logger.LogError(erro, "Ocorreu uma falha ao tentar remover as faturas: ");
    }
  }
}