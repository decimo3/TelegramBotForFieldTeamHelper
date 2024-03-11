namespace telbot.handle;
public static class HandleAnnouncement
{
  private static int CINCO_MINUTOS = 1_000 * 60 * 5;
  public static async void Vencimento(HandleMessage msg, Configuration cfg)
  {
    while(true)
    {
    while(true)
    {
      if(!System.IO.File.Exists(cfg.LOCKFILE)) break;
      else System.Threading.Thread.Sleep(1_000);
    }
    Console.WriteLine($"< {DateTime.Now} Manager: Comunicado para todos - Vencimentos");
    System.IO.File.Create(cfg.LOCKFILE).Close();
    var relatorio_resultado = Temporary.executar(cfg, "vencimento", "7");
    var relatorio_caminho = cfg.CURRENT_PATH + "\\tmp\\temporario.csv";
    if(!relatorio_resultado.Any())
    {
      Temporary.ConsoleWriteError("Erro ao gerar o relatório de notas em aberto!\nTentaremos novamente daqui a cinco minutos");
      System.IO.File.Delete(cfg.LOCKFILE);
      System.Threading.Thread.Sleep(CINCO_MINUTOS);
      continue;
    }
    if(relatorio_resultado.First().StartsWith("ERRO:"))
    {
      Temporary.ConsoleWriteError("Erro ao gerar o relatório de notas em aberto!\nTentaremos novamente daqui a cinco minutos");
      System.IO.File.Delete(cfg.LOCKFILE);
      System.Threading.Thread.Sleep(CINCO_MINUTOS);
      continue;
    }
    if(!System.IO.File.Exists(relatorio_caminho))
    {
      Temporary.ConsoleWriteError("Erro ao gerar o relatório de notas em aberto!\nTentaremos novamente daqui a cinco minutos");
      System.IO.File.Delete(cfg.LOCKFILE);
      System.Threading.Thread.Sleep(CINCO_MINUTOS);
      continue;
    }
    Stream relatorio_arquivo = System.IO.File.OpenRead(relatorio_caminho);
    if(relatorio_arquivo.Length == 0)
    {
      relatorio_arquivo.Close();
      Temporary.ConsoleWriteError("Erro ao gerar o relatório de notas em aberto!\nTentaremos novamente daqui a cinco minutos");
      System.IO.File.Delete(cfg.LOCKFILE);
      System.Threading.Thread.Sleep(CINCO_MINUTOS);
      continue;
    }
    var relatorio_mensagem = String.Join('\n', relatorio_resultado);
    var padrao = @"([0-9]{2})/([0-9]{2})/([0-9]{4}) ([0-9]{2}):([0-9]{2}):([0-9]{2})";
    var relatorio_filename = new System.Text.RegularExpressions.Regex(padrao).Match(relatorio_mensagem).Value;
    var padrao_trocar = @"$3-$2-$1_$4-$5-$6\.csv";
    relatorio_filename = new System.Text.RegularExpressions.Regex(padrao).Replace(relatorio_filename, padrao_trocar);
    var relatorio_identificador = await msg.SendDocumentAsyncWraper(cfg.ID_ADM_BOT, relatorio_arquivo, relatorio_filename);
    if(relatorio_identificador == String.Empty)
    {
      relatorio_arquivo.Close();
      Temporary.ConsoleWriteError("Erro ao enviar o relatório de notas em aberto!\nTentaremos novamente daqui a cinco minutos");
      System.IO.File.Delete(cfg.LOCKFILE);
      System.Threading.Thread.Sleep(CINCO_MINUTOS);
      continue;
    }
    var usuarios = Database.recuperarUsuario();
    var tasks = new List<Task>();
    foreach (var usuario in usuarios)
    {
      DateTime expiracao = usuario.update_at.AddDays(cfg.DIAS_EXPIRACAO);
      if(System.DateTime.Compare(DateTime.Now, expiracao) > 0) continue;
      tasks.Add(msg.sendTextMesssageWraper(usuario.id, relatorio_mensagem, true, false));
      if(usuario.id == cfg.ID_ADM_BOT) continue;
      if(usuario.has_privilege)
      {
        tasks.Add(msg.SendDocumentAsyncWraper(usuario.id, relatorio_identificador));
      }
    }
    await Task.WhenAll(tasks);
    relatorio_arquivo.Close();
    System.IO.File.Delete(relatorio_caminho);
    System.IO.File.Delete(cfg.LOCKFILE);
    break;
    }
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
    if(comunicado_mensagem.Length == 0) has_txt = false;
    Stream comunicado_imagem = has_jpg ? File.OpenRead(imagem_caminho) : Stream.Null;
    if(comunicado_imagem.Length == 0) has_jpg = false;
    Stream comunicado_video = has_mp4 ? File.OpenRead(videoclipe_caminho) : Stream.Null;
    if(comunicado_video.Length == 0) has_mp4 = false;
    
    var photo_id = has_jpg ? await msg.SendPhotoAsyncWraper(cfg.ID_ADM_BOT, comunicado_imagem) : String.Empty;
    var video_id = has_mp4 ? await msg.SendVideoAsyncWraper(cfg.ID_ADM_BOT, comunicado_video) : String.Empty;

    Console.WriteLine($"< {DateTime.Now} Manager: Comunicado para todos - Comunicado");
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