namespace telbot;
public static class Temporary
{
  private static string PYTHON_PATH = @"C:\Users\ruan.camello\scoop\apps\python\current\python.exe";
  private static string PYTHON_FILE = @"C:\Users\ruan.camello\Documents\Development\Automacao\sap.py";
  private static string EXCEL_FILE = @"C:\Users\ruan.camello\Documents\Development\Automacao\xls.py";
  public static List<string> executar(string aplicacao, string informacao)
  {
    using(var proc = new System.Diagnostics.Process{
      StartInfo = new System.Diagnostics.ProcessStartInfo
        {
          FileName = PYTHON_PATH,
          Arguments = $"{PYTHON_FILE} {aplicacao.ToLower()} {informacao}",
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
  public static void executar(List<string> listaValoresSeparadosPorTabulacao)
  {
    string valoresSeparadosPorTabulacao = string.Join("\n", listaValoresSeparadosPorTabulacao.ToArray());
    using(var proc = new System.Diagnostics.Process{
      StartInfo = new System.Diagnostics.ProcessStartInfo
        {
          FileName = PYTHON_PATH,
          Arguments = $"{EXCEL_FILE} {valoresSeparadosPorTabulacao}",
          UseShellExecute = false,
          RedirectStandardOutput = true,
          CreateNoWindow = true
        }})
      {
        proc.Start();
      }
  }
}