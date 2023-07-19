namespace telbot;
public static class Temporary
{
  public static List<string> executar(Configuration cfg, string aplicacao, string informacao)
  {
    string[] args = System.Environment.GetCommandLineArgs();
    using(var proc = new System.Diagnostics.Process{
      StartInfo = new System.Diagnostics.ProcessStartInfo
        {
          FileName = cfg.SAP_SCRIPT,
          Arguments = $"{aplicacao} {informacao} {cfg.INSTANCIA}",
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
}