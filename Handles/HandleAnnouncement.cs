namespace telbot.handle;
public static class HandleAnnouncement
{
  public static async void Vencimento(HandleMessage msg, Configuration cfg)
  {
    System.IO.File.Create(cfg.LOCKFILE).Close();
    var relatorio_resultado = Temporary.executar(cfg, "vencimento", "7");
    var relatorio_arquivo = cfg.CURRENT_PATH + "\\tmp\\temporario.csv";
    if(!System.IO.File.Exists(relatorio_arquivo)) return;
    if(!relatorio_resultado.Any()) return;
    if(relatorio_resultado.First().StartsWith("ERRO:")) return;
    var relatorio_mensagem = String.Join('\n', relatorio_resultado);
    var padrao = @"([0-9]{2})/([0-9]{2})/([0-9]{4}) ([0-9]{2}):([0-9]{2}):([0-9]{2})";
    var relatorio_filename = new System.Text.RegularExpressions.Regex(padrao).Match(relatorio_mensagem).Value;
    Stream relatorio_stream = System.IO.File.OpenRead(relatorio_arquivo);
    Console.WriteLine($"< {DateTime.Now} Manager: ForAllAnnouncement");
    var usuarios = Database.recuperarUsuario();
    var tasks = new List<Task>();
    foreach (var usuario in usuarios)
    {
      tasks.Add(msg.sendTextMesssageWraper(usuario.id, relatorio_mensagem));
      if(usuario.has_privilege)
      {
        relatorio_stream.Position = 0;
        tasks.Add(msg.SendDocumentAsyncWraper(usuario.id, relatorio_stream, relatorio_filename));
      }
    }
    await Task.WhenAll(tasks);
    relatorio_stream.Close();
    System.IO.File.Delete(relatorio_arquivo);
    System.IO.File.Delete(cfg.LOCKFILE);
  }
  public static async void Comunicado(HandleMessage msg, Configuration cfg)
  {
    System.IO.File.Create(cfg.LOCKFILE).Close();
    var comunicado_arquivo = cfg.CURRENT_PATH + "\\comunicado.txt";
    if(!System.IO.File.Exists(comunicado_arquivo)) return;
    var comunicado_mensagem = System.IO.File.ReadAllText(comunicado_arquivo);
    Console.WriteLine($"< {DateTime.Now} Manager: Comunicado para todos:");
    var usuarios = Database.recuperarUsuario();
    var tasks = new List<Task>();
    foreach (var usuario in usuarios)
    {
      tasks.Add(msg.sendTextMesssageWraper(usuario.id, comunicado_mensagem));
    }
    await Task.WhenAll(tasks);
    Console.WriteLine(comunicado_mensagem);
    System.IO.File.Delete(comunicado_arquivo);
    System.IO.File.Delete(cfg.LOCKFILE);
  }
}