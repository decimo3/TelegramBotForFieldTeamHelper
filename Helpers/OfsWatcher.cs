using telbot.Services;
namespace telbot.Helpers;
public static partial class OfsHandle
{
  public static void Enrol(String application, Int64 information, DateTime received_at)
  {
    var texto = String.Empty;
    var cfg = Configuration.GetInstance();
    var argumentos = new String[] {
      application,
      information.ToString(),
      received_at.ToLocalTime().ToString("yyyyMMddHHmmss")
    };
    var argumentos_texto = String.Join(' ', argumentos);
    while (true)
    {
      texto = System.IO.File.ReadAllText("ofs.lock", System.Text.Encoding.UTF8);
      if (texto.Length > 0)
      {
        System.Threading.Thread.Sleep(cfg.TASK_DELAY);
        continue;
      }
      System.IO.File.WriteAllText("ofs.lock", argumentos_texto);
      texto = System.IO.File.ReadAllText("ofs.lock", System.Text.Encoding.UTF8);
      if (texto == argumentos_texto) break;
    }
  }
}