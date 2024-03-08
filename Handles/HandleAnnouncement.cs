namespace telbot.handle;
public static class HandleAnnouncement
{
  public static async void Vencimento(HandleMessage msg, Configuration cfg)
  {
    while(true)
    {
      if(!System.IO.File.Exists(cfg.LOCKFILE)) break;
      else System.Threading.Thread.Sleep(1_000);
    }
    Console.WriteLine($"< {DateTime.Now} Manager: ForAllAnnouncement - Vencimentos");
    System.IO.File.Create(cfg.LOCKFILE).Close();
    var relatorio_resultado = Temporary.executar(cfg, "vencimento", "7");
    var relatorio_arquivo = cfg.CURRENT_PATH + "\\tmp\\temporario.csv";
    if(!System.IO.File.Exists(relatorio_arquivo))
    {
      System.IO.File.Delete(cfg.LOCKFILE);
      return;
    }
    if(!relatorio_resultado.Any())
    {
      System.IO.File.Delete(cfg.LOCKFILE);
      return;
    }
    if(relatorio_resultado.First().StartsWith("ERRO:"))
    {
      System.IO.File.Delete(cfg.LOCKFILE);
      return;
    }
    var relatorio_mensagem = String.Join('\n', relatorio_resultado);
    var padrao = @"([0-9]{2})/([0-9]{2})/([0-9]{4}) ([0-9]{2}):([0-9]{2}):([0-9]{2})";
    var relatorio_filename = new System.Text.RegularExpressions.Regex(padrao).Match(relatorio_mensagem).Value;
    var padrao_trocar = @"$3-$2-$1_$4-$5-$6\.csv";
    relatorio_filename = new System.Text.RegularExpressions.Regex(padrao).Replace(relatorio_filename, padrao_trocar);
    Stream relatorio_stream = System.IO.File.OpenRead(relatorio_arquivo);
    var usuarios = Database.recuperarUsuario();
    var tasks = new List<Task>();
    foreach (var usuario in usuarios)
    {
      DateTime expiracao = usuario.update_at.AddDays(cfg.DIAS_EXPIRACAO);
      if(System.DateTime.Compare(DateTime.Now, expiracao) > 0) continue;
      tasks.Add(msg.sendTextMesssageWraper(usuario.id, relatorio_mensagem, true, false));
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
    var mensagem_caminho = cfg.CURRENT_PATH + "\\comunicado.txt";
    var imagem_caminho = cfg.CURRENT_PATH + "\\comunicado.jpg";
    var videoclipe_caminho = cfg.CURRENT_PATH + "\\comunicado.mp4";

    var has_txt = System.IO.File.Exists(mensagem_caminho);
    var has_jpg = System.IO.File.Exists(imagem_caminho);
    var has_mp4 = System.IO.File.Exists(videoclipe_caminho);
    
    if(!has_txt && !has_jpg && !has_mp4) return;
    
    System.IO.File.Create(cfg.LOCKFILE).Close();
    
    var comunicado_mensagem = has_txt ? File.ReadAllText(mensagem_caminho) : String.Empty;
    Stream comunicado_imagem = has_jpg ? File.OpenRead(imagem_caminho) : Stream.Null;
    Stream comunicado_video = has_mp4 ? File.OpenRead(videoclipe_caminho) : Stream.Null;
    
    var photo_id = has_jpg ? await msg.SendPhotoAsyncWraper(cfg.ID_ADM_BOT, comunicado_imagem) : String.Empty;
    var video_id = has_mp4 ? await msg.SendVideoAsyncWraper(cfg.ID_ADM_BOT, comunicado_video) : String.Empty;

    Console.WriteLine($"< {DateTime.Now} Manager: Comunicado para todos:");
    var usuarios = Database.recuperarUsuario();
    var tasks = new List<Task>();
    foreach (var usuario in usuarios)
    {
      DateTime expiracao = usuario.update_at.AddDays(cfg.DIAS_EXPIRACAO);
      if(System.DateTime.Compare(DateTime.Now, expiracao) > 0) continue;
      if(has_txt) tasks.Add(msg.sendTextMesssageWraper(usuario.id, comunicado_mensagem, true, false));
      if(usuario.id == cfg.ID_ADM_BOT) continue;
      if(has_jpg && (photo_id != String.Empty)) tasks.Add(msg.SendPhotoAsyncWraper(usuario.id, photo_id));
      if(has_mp4 && (video_id != String.Empty)) tasks.Add(msg.SendVideoAsyncWraper(usuario.id, video_id));
    }
    await Task.WhenAll(tasks);
    comunicado_imagem.Close();
    comunicado_video.Close();
    if(has_txt)
    {
      Console.WriteLine(comunicado_mensagem);
      System.IO.File.Delete(mensagem_caminho);
    }
    if(has_jpg)
    {
      Console.WriteLine("Enviada imagem do comunicado!");
      System.IO.File.Delete(imagem_caminho);
    }
    if(has_mp4)
    {
      Console.WriteLine("Enviado video do comunicado!");
      System.IO.File.Delete(videoclipe_caminho);
    }
    System.IO.File.Delete(cfg.LOCKFILE);
  }
}