namespace telbot.Helpers
{
  public static class ConsoleWrapper
  {
    public static void Read(DateTime horario, String mensagem)
    {
      Console.WriteLine($"> {horario} {Entidade.Usuario} - {mensagem}");
    }
    public static void Write(Entidade entidade, String mensagem)
    {
      Console.WriteLine($"< {DateTime.Now} {entidade} - {mensagem}");
    }
    public static void Error(Entidade entidade, Exception erro)
    {
      Console.WriteLine();
      Console.BackgroundColor = ConsoleColor.Red;
      Console.WriteLine($"< {DateTime.Now} {entidade} - {erro.Message}");
      if(Print()) Console.WriteLine($"< {DateTime.Now} {entidade} - {erro.StackTrace}");
      Console.BackgroundColor = ConsoleColor.Black;
      Console.WriteLine();
    }
    public static void Debug(Entidade entidade, String mensagem)
    {
      if(!Print()) return;
      Console.WriteLine();
      Console.BackgroundColor = ConsoleColor.White;
      Console.ForegroundColor = ConsoleColor.Black;
      Console.WriteLine($"< {DateTime.Now} {entidade} - {mensagem}");
      Console.BackgroundColor = ConsoleColor.Black;
      Console.ForegroundColor = ConsoleColor.White;
      Console.WriteLine();
    }
    public static bool Print()
    {
      var args = System.Environment.GetCommandLineArgs();
      return args.Contains("--em-desenvolvimento");
    }
  }
  public enum Entidade {Usuario, Chatbot, Manager, Updater, Messenger, Advertiser, Recovery, Executor, SoireeAsync, CookerAsync, WaiterAsync}
}
