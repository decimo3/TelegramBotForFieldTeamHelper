using Microsoft.Extensions.Logging;
using telbot.Services;
namespace telbot.Helpers;
public class Executor
{
  public static String? Executar(String aplicacao, String[] argumentos, Boolean expect_return) =>
      Executor.ExecutarAsync(aplicacao, argumentos, expect_return).Result;

  public static async Task<String?> ExecutarAsync(String aplicacao, String[] argumentos, Boolean expect_return)
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
    if(!expect_return) return null;
    // Consome stdout e stderr simultaneamente para evitar deadlocks.
    var outputTask = processo.StandardOutput.ReadToEndAsync();
    var errorTask = processo.StandardError.ReadToEndAsync();
    using var cts = new CancellationTokenSource(cfg.SAP_ESPERA);
    try
    {
      await processo.WaitForExitAsync(cts.Token);
    }
    catch (OperationCanceledException)
    {
      // Encerra o processo principal e seus descendentes.
      try
      {
        if (!processo.HasExited) processo.Kill(entireProcessTree: true);
      }
      catch (Exception ex)
      {
        logger.LogWarning(ex, "Não foi possível encerrar {application}", aplicacao);
      }
      await processo.WaitForExitAsync();
      await Task.WhenAll(outputTask, errorTask);
      throw new TimeoutException(
        $"Tempo limite excedido ao executar {aplicacao}.");
    }
    var output = await outputTask;
    var errput = await errorTask;
    if(processo.ExitCode == 0)
    {
      var filteredLines = output
        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
        .Where(line => !String.IsNullOrWhiteSpace(line));
      output = String.Join(Environment.NewLine, filteredLines);
      logger.LogDebug(output);
      return output;
    }
    var erro = new Exception(errput);
    logger.LogError(erro, "Falha ao executar a aplicação {application}", aplicacao);
    return errput;
  }
}
