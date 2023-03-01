namespace telbot;
public static class Temporary
{
  private static string PYTHON_PATH = @"C:\Users\ruan.camello\scoop\apps\python\current\python.exe";
  private static string PYTHON_FILE = @"C:\Users\ruan.camello\Documents\Development\Automacao\sap.py";
  public static string executar(string aplicacao, string informacao)
  {
    using(var proc = new System.Diagnostics.Process{
      StartInfo = new System.Diagnostics.ProcessStartInfo
        {
          FileName = PYTHON_PATH,
          Arguments = $"{PYTHON_FILE} {aplicacao.ToLower()} {informacao.ToLower()}",
          UseShellExecute = false,
          RedirectStandardOutput = true,
          CreateNoWindow = true
        }})
      {
        string? linha = "";
        proc.Start();
        while (!proc.StandardOutput.EndOfStream)
        {
          linha = proc.StandardOutput.ReadLine();
          // do something with line
        }
        return linha;
      }
  }
}