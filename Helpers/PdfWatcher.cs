using telbot.Services;
using telbot.models;
using Microsoft.Extensions.Logging;
namespace telbot.Helpers;
public partial class PdfHandle
{
  public async void Watch()
  {
    logger.LogDebug("Monitor de faturas iniciado!");
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
          if(!faturas.TryGetValue(filename, out pdfsModel? registro))
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
            faturas.TryAdd(filename, fatura);
          }
          if (registro is null)
          {
            continue;
          }
          if(registro.timestamp.AddMilliseconds(cfg.SAP_ESPERA) < DateTime.Now)
          {
            Remove(new List<pdfsModel>(){registro});
          }
        }
      }
      catch (System.Exception erro)
      {
        logger.LogError(erro, "Ocorreu uma falha ao monitorar o diretório de faturas: ");
      }
    }
  }
  public void Remove(List<pdfsModel> faturas_info)
  {
    try
    {
      foreach (var fatura in faturas_info)
      {
        var filepath = System.IO.Path.Combine(
          cfg.TEMP_FOLDER, fatura.filename);
        System.IO.File.Delete(filepath);
        faturas.TryRemove(fatura.filename, out _);
        logger.LogDebug("Excluída fatura {fatura}", fatura.filename);
      }
    }
    catch (System.Exception erro)
    {
      logger.LogError(erro, "Ocorreu uma falha ao tentar remover as faturas!");
    }
  }
}