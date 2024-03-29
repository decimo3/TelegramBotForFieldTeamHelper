using System.Diagnostics;
namespace telbot;
public static class Temporary
{
  public static List<string> executar(Configuration cfg, string aplicacao, long informacao)
  {
    var argumentos = $"{aplicacao} {informacao} {cfg.INSTANCIA}";
    if(cfg.SAP_RESTRITO) argumentos += " --sap-restrito";
    var proc = new System.Diagnostics.Process();
    var startInfo = new System.Diagnostics.ProcessStartInfo
      {
        FileName = cfg.SAP_SCRIPT,
        Arguments = argumentos,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        CreateNoWindow = true
      };
    proc.StartInfo = startInfo;
    var linhas = new List<string>();
    proc.Start();
    var tempo = new System.Threading.Timer(state => {
      if(!proc.HasExited)
      {
        var mos = Temporary.GetChildProcesses(proc);
        foreach (var mo in mos)
        {
          mo.Kill();
        }
      }
    }, null, cfg.ESPERA, Timeout.Infinite);
    while (!proc.StandardOutput.EndOfStream)
    {
      var linha = proc.StandardOutput.ReadLine();
      if(linha != null && linha != String.Empty) linhas.Add(linha);
    }
    tempo.Dispose();
    proc.Dispose();
    return linhas;
  }
  private static IEnumerable<Process> GetChildProcesses(this Process process)
  {
    var children = new List<Process>();
    var queryProcess = $"Select * From Win32_Process Where ParentProcessID={process.Id}";
    var mos = new System.Management.ManagementObjectSearcher(queryProcess);
    foreach (var mo in mos.Get())
    {
        children.Add(Process.GetProcessById(Convert.ToInt32(mo["ProcessID"])));
    }
    return children;
  }
  public static List<string> executar(Configuration cfg, List<string> listaValoresSeparadosPorTabulacao)
  {
    string textoValoresSeparadosPorTabulacao = string.Join("\n", listaValoresSeparadosPorTabulacao);
    using(var proc = new System.Diagnostics.Process{
      StartInfo = new System.Diagnostics.ProcessStartInfo
        {
          FileName = cfg.IMG_SCRIPT,
          Arguments = $"\"{textoValoresSeparadosPorTabulacao}\"",
          UseShellExecute = false,
          RedirectStandardOutput = true,
          CreateNoWindow = true
        }})
      {
        var linha = new List<string>();
        proc.Start();
        while (!proc.StandardOutput.EndOfStream)
        {
          linha.Add(proc.StandardOutput.ReadLine()!);
        }
        return linha;
      }
  }
  public static void extratoDiario(Configuration cfg)
  {
    var argumentos = $"-header -csv database.db \"SELECT * FROM logsModel;\"";
    using(var proc = new System.Diagnostics.Process{
      StartInfo = new System.Diagnostics.ProcessStartInfo
        {
          FileName = "sqlite3",
          Arguments = argumentos, //WHERE DATE(create_at) == DATE('{DateTime.Now.ToString("dd-MM-yyyy")}')
          UseShellExecute = false,
          RedirectStandardOutput = true,
          CreateNoWindow = true
        }})
      {
        var linha = new List<string>();
        proc.Start();
        while (!proc.StandardOutput.EndOfStream)
        {
          linha.Add(proc.StandardOutput.ReadLine()!);
        }
        System.IO.File.WriteAllLines($"{cfg.CURRENT_PATH}/dados.csv", linha);
      }
  }
  public static void ConsoleWriteError(String output)
  {
    Console.BackgroundColor = ConsoleColor.Red;
    Console.Beep();
    Console.WriteLine(output);
    Console.BackgroundColor = ConsoleColor.Black;
  }
}