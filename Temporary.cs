namespace telbot;
public static class Temporary
{
  public static string CURRENT_PATH = System.IO.Directory.GetCurrentDirectory();
  private static string SAP_SCRIPT = CURRENT_PATH + @"\sap.exe";
  private static string IMG_SCRIPT = CURRENT_PATH + @"\img.exe";
  public static List<string> executar(string aplicacao, string informacao)
  {
    string[] args = System.Environment.GetCommandLineArgs();
    int instancia = args.Contains("--in-development") ? 1 : 0;
    using(var proc = new System.Diagnostics.Process{
      StartInfo = new System.Diagnostics.ProcessStartInfo
        {
          FileName = SAP_SCRIPT,
          Arguments = $"{aplicacao} {informacao} {instancia}",
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
  public static List<string> executar(List<string> listaValoresSeparadosPorTabulacao)
  {
    string textoValoresSeparadosPorTabulacao = string.Join("\n", listaValoresSeparadosPorTabulacao);
    using(var proc = new System.Diagnostics.Process{
      StartInfo = new System.Diagnostics.ProcessStartInfo
        {
          FileName = IMG_SCRIPT,
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
}