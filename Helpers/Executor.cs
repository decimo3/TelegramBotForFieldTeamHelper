using System.Diagnostics;
using Microsoft.Extensions.Logging;
using telbot.Services;
namespace telbot.Helpers;
public class Executor
{
  private static IEnumerable<Process> GetChildProcesses(Process process)
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
  public static String? Executar(String aplicacao, String[] argumentos, Boolean expect_return)
  {
    var cfg = Configuration.GetInstance();
    var logger = Logger.GetInstance<Executor>();
    var argumentos_texto = String.Join(' ', argumentos);
    logger.LogDebug("Executando {application} {arguments}", aplicacao, argumentos_texto);
    using var processo = new System.Diagnostics.Process();
    var startInfo = new System.Diagnostics.ProcessStartInfo()
    {
      FileName = aplicacao,
      Arguments = argumentos_texto,
      UseShellExecute = !expect_return,
      RedirectStandardOutput = expect_return,
      RedirectStandardError = expect_return,
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
      var output = processo.StandardOutput.ReadToEnd();
      var errput = processo.StandardError.ReadToEnd();
      processo.WaitForExit();
      if(processo.ExitCode == 0)
      {
        logger.LogDebug(output);
        return output;
      }
      else
      {
        var erro = new Exception(errput);
        logger.LogError(erro, "Falha ao executar a aplicação {application}", aplicacao);
        return errput;
      }
    }
    return null;
  }
}