namespace telbot;
public static class Temporary
{
  private static string PYTHON_PATH = @"C:\Users\ruan.camello\scoop\apps\python\current\python.exe";
  private static string SAP_SCRIPT = @"C:\Users\ruan.camello\Documents\Development\Automacao\src\sap.py";
  private static string IMG_SCRIPT = @"C:\Users\ruan.camello\Documents\Development\Automacao\src\img.py";
  public static List<string> executar(string aplicacao, string informacao)
  {
    using(var proc = new System.Diagnostics.Process{
      StartInfo = new System.Diagnostics.ProcessStartInfo
        {
          FileName = PYTHON_PATH,
          Arguments = $"{SAP_SCRIPT} {aplicacao} {informacao}",
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
          FileName = PYTHON_PATH,
          Arguments = $"{IMG_SCRIPT} \"{textoValoresSeparadosPorTabulacao}\"",
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