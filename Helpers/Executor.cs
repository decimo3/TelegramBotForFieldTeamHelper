using System.Diagnostics;
using telbot.Helpers;
using telbot.Services;
namespace telbot;
public static class Executor
{
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
  public static String Executar(String aplicacao, String[] argumentos, Boolean expect_return)
  {
    var linhas = new List<String>();
    var cfg = Configuration.GetInstance();
    var argumentos_texto = String.Join(' ', argumentos);
    ConsoleWrapper.Debug(Entidade.Executor, aplicacao + " " + argumentos_texto);
    using var processo = new System.Diagnostics.Process();
    var startInfo = new System.Diagnostics.ProcessStartInfo()
    {
      FileName = aplicacao,
      Arguments = argumentos_texto,
      UseShellExecute = !expect_return,
      RedirectStandardOutput = expect_return,
      CreateNoWindow = true
    };
    processo.StartInfo = startInfo;
    processo.Start();
    using var tempo = new System.Threading.Timer(state => {
      if(!processo.HasExited)
      {
        var mos = GetChildProcesses(processo);
        foreach (var mo in mos)
        {
          mo.Kill();
        }
      }
    }, null, cfg.SAP_ESPERA, Timeout.Infinite);
    if(expect_return)
    {
      while (!processo.StandardOutput.EndOfStream)
      {
        var linha = processo.StandardOutput.ReadLine();
        if(!String.IsNullOrEmpty(linha)) linhas.Add(linha);
      }
    }
    var retorno = String.Join('\n', linhas);
    ConsoleWrapper.Debug(Entidade.Executor, retorno);
    return retorno;
  }
}