using telbot.Services;
using telbot.models;
namespace telbot.Helpers;
public static partial class PdfHandle
{
  public static async void Watch()
  {
    var cfg = Configuration.GetInstance();
    var database = Database.GetInstance();
    while (true)
    {
      try
      {
        
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
            database.InserirFatura(new pdfsModel() {
              filename = filename,
              instalation = instalation,
              timestamp = timestamp,
              status = pdfsModel.Status.wait
            });
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
        ConsoleWrapper.Error(Entidade.Executor, erro);
      }
    }
  }
}