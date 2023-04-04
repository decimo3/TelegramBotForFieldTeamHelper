namespace telbot;
public static class Temporary
{
  public static string USER_PATH = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
  private static string PYTHON_PATH()
  {
    if(System.OperatingSystem.IsWindows()) return (USER_PATH + @"\scoop\apps\python\current\python.exe");
    if(System.OperatingSystem.IsLinux()) return (USER_PATH + @"/.asdf/shims/python");
    throw new System.Exception("Sistema operacional não configurado!");
  }
  private static string SAP_SCRIPT()
  {
    if(System.OperatingSystem.IsWindows()) return (USER_PATH + @"\Documents\Development\Automacao\src\sap.py");
    if(System.OperatingSystem.IsLinux()) return (USER_PATH + @"/Documents/Development/Automacao/src/sap.py");
    throw new System.Exception("Sistema operacional não configurado!");
  }
  private static string IMG_SCRIPT()
  {
    if(System.OperatingSystem.IsWindows()) return (USER_PATH + @"\Documents\Development\Automacao\src\img.py");
    if(System.OperatingSystem.IsLinux()) return (USER_PATH + @"/Documents/Development/Automacao/src/img.py");
    throw new System.Exception("Sistema operacional não configurado!");
  }
  public static List<string> executar(string aplicacao, string informacao)
  {
    using(var proc = new System.Diagnostics.Process{
      StartInfo = new System.Diagnostics.ProcessStartInfo
        {
          FileName = PYTHON_PATH(),
          Arguments = $"{SAP_SCRIPT()} {aplicacao} {informacao}",
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
          FileName = PYTHON_PATH(),
          Arguments = $"{IMG_SCRIPT()} \"{textoValoresSeparadosPorTabulacao}\"",
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