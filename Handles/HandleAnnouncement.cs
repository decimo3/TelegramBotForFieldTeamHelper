using System.Data;
using telbot.Helpers;
namespace telbot.handle;
public static class HandleAnnouncement
{
  private static int CINCO_MINUTOS = 1_000 * 60 * 5;
  public static async void Vencimento(HandleMessage msg, Configuration cfg, String aplicacao, Int32 prazo)
  {
    while(true)
    {
    while(true)
    {
      if(!System.IO.File.Exists(cfg.LOCKFILE)) break;
      else System.Threading.Thread.Sleep(1_000);
    }
    Console.WriteLine($"< {DateTime.Now} Manager: Comunicado para todos - {aplicacao}");
    System.IO.File.Create(cfg.LOCKFILE).Close();
    var relatorio_resultado = Temporary.executar(cfg, aplicacao, prazo);
    var relatorio_caminho = cfg.CURRENT_PATH + "\\tmp\\temporario.csv";
    if(!relatorio_resultado.Any())
    {
      ConsoleWrapper.Error(Entidade.Advertiser, new Exception("Erro ao gerar o relatório de notas em aberto!\nTentaremos novamente daqui a cinco minutos"));
      System.IO.File.Delete(cfg.LOCKFILE);
      System.Threading.Thread.Sleep(CINCO_MINUTOS);
      continue;
    }
    if(relatorio_resultado.First().StartsWith("ERRO:"))
    {
      ConsoleWrapper.Error(Entidade.Advertiser, new Exception("Erro ao gerar o relatório de notas em aberto!\nTentaremos novamente daqui a cinco minutos"));
      System.IO.File.Delete(cfg.LOCKFILE);
      System.Threading.Thread.Sleep(CINCO_MINUTOS);
      continue;
    }
    if(!System.IO.File.Exists(relatorio_caminho))
    {
      ConsoleWrapper.Error(Entidade.Advertiser, new Exception("Erro ao gerar o relatório de notas em aberto!\nTentaremos novamente daqui a cinco minutos"));
      System.IO.File.Delete(cfg.LOCKFILE);
      System.Threading.Thread.Sleep(CINCO_MINUTOS);
      continue;
    }
    Stream relatorio_arquivo = System.IO.File.OpenRead(relatorio_caminho);
    if(relatorio_arquivo.Length == 0)
    {
      relatorio_arquivo.Close();
      ConsoleWrapper.Error(Entidade.Advertiser, new Exception("Erro ao gerar o relatório de notas em aberto!\nTentaremos novamente daqui a cinco minutos"));
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
      ConsoleWrapper.Error(Entidade.Advertiser, new Exception("Erro ao enviar o relatório de notas em aberto!\nTentaremos novamente daqui a cinco minutos"));
      System.IO.File.Delete(cfg.LOCKFILE);
      System.Threading.Thread.Sleep(CINCO_MINUTOS);
      continue;
    }
    var usuarios = Database.recuperarUsuario(u =>
      (
        u.has_privilege == UsersModel.userLevel.proprietario ||
        u.has_privilege == UsersModel.userLevel.administrador ||
        (u.has_privilege == UsersModel.userLevel.controlador && u.update_at.AddDays(cfg.DIAS_EXPIRACAO) > DateTime.Now) ||
        (u.has_privilege == UsersModel.userLevel.supervisor && u.update_at.AddDays(cfg.DIAS_EXPIRACAO * 3) > DateTime.Now)
      )
    );
    
    ConsoleWrapper.Debug(Entidade.Advertiser, $"Usuários selecionados: {usuarios.Count()}");
    await Comunicado(usuarios, msg, cfg, cfg.ID_ADM_BOT, relatorio_mensagem, null, null, relatorio_identificador);

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
    var documento_caminho = cfg.CURRENT_PATH + "\\comunicado.pdf";

    var has_txt = System.IO.File.Exists(mensagem_caminho);
    var has_jpg = System.IO.File.Exists(imagem_caminho);
    var has_mp4 = System.IO.File.Exists(videoclipe_caminho);
    var has_doc = System.IO.File.Exists(documento_caminho);
    
    if(!has_txt && !has_jpg && !has_mp4 && !has_doc) return;
    
    System.IO.File.Create(cfg.LOCKFILE).Close();
    
    var comunicado_mensagem = has_txt ? File.ReadAllText(mensagem_caminho) : null;
    if(comunicado_mensagem == null || comunicado_mensagem.Length == 0) has_txt = false;
    Stream comunicado_imagem = has_jpg ? File.OpenRead(imagem_caminho) : Stream.Null;
    if(comunicado_imagem.Length == 0) has_jpg = false;
    Stream comunicado_video = has_mp4 ? File.OpenRead(videoclipe_caminho) : Stream.Null;
    if(comunicado_video.Length == 0) has_mp4 = false;
    Stream comunicado_documento = has_doc ? File.OpenRead(documento_caminho) : Stream.Null;
    if(documento_caminho.Length == 0) has_doc = false;
    
    var photo_id = has_jpg ? await msg.SendPhotoAsyncWraper(cfg.ID_ADM_BOT, comunicado_imagem) : null;
    var video_id = has_mp4 ? await msg.SendVideoAsyncWraper(cfg.ID_ADM_BOT, comunicado_video) : null;
    var doc_id = has_doc ? await msg.SendDocumentAsyncWraper(cfg.ID_ADM_BOT, comunicado_documento, $"comunicado_{DateTime.Now.ToString("yyyyMMdd")}.pdf") : null;

    var usuarios = Database.recuperarUsuario(u =>
      (
        u.has_privilege == UsersModel.userLevel.proprietario ||
        u.has_privilege == UsersModel.userLevel.administrador ||
        u.has_privilege == UsersModel.userLevel.comunicador ||
        (u.has_privilege == UsersModel.userLevel.eletricista && u.update_at.AddDays(cfg.DIAS_EXPIRACAO) > DateTime.Now) ||
        (u.has_privilege == UsersModel.userLevel.controlador && u.update_at.AddDays(cfg.DIAS_EXPIRACAO) > DateTime.Now) ||
        (u.has_privilege == UsersModel.userLevel.supervisor && u.update_at.AddDays(cfg.DIAS_EXPIRACAO * 3) > DateTime.Now)
      )
    );

    ConsoleWrapper.Debug(Entidade.Advertiser, $"Usuários selecionados: {usuarios.Count()}");
    await Comunicado(usuarios, msg, cfg, cfg.ID_ADM_BOT, comunicado_mensagem, photo_id, video_id, doc_id);

    comunicado_imagem.Close();
    comunicado_video.Close();
    comunicado_documento.Close();
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
    if(has_doc)
    {
      Console.WriteLine("Enviado documento do comunicado!");
      System.IO.File.Delete(documento_caminho);
    }
    System.IO.File.Delete(cfg.LOCKFILE);
  }
  public static async Task Comunicado(List<UsersModel> usuarios, HandleMessage msg, Configuration cfg, long myself, string? text, string? image_id, string? video_id, string? doc_id)
  {
    Console.WriteLine($"< {DateTime.Now} Manager: Comunicado para todos - Comunicado");
    var tasks = new List<Task>();
    foreach (var usuario in usuarios)
    {
      if(usuario.id == myself) continue;
      if(text != null) tasks.Add(msg.sendTextMesssageWraper(usuario.id, text, true, false));
      if(image_id != null) tasks.Add(msg.SendPhotoAsyncWraper(usuario.id, image_id));
      if(video_id != null) tasks.Add(msg.SendVideoAsyncWraper(usuario.id, video_id));
      if(doc_id != null) tasks.Add(msg.SendDocumentAsyncWraper(usuario.id, doc_id));
    }
    await Task.WhenAll(tasks);
  }
  public static async void Monitorado(HandleMessage msg, Configuration cfg)
  {
    while(true)
    {
    System.Threading.Thread.Sleep(CINCO_MINUTOS);
    if(DateTime.Now.DayOfWeek != DayOfWeek.Saturday && DateTime.Now.DayOfWeek != DayOfWeek.Sunday)
      {
        var hora_agora = DateTime.Now.Hour;
        if(hora_agora >= 7 && hora_agora <= 22)
        {
      var mensagem_caminho = cfg.CURRENT_PATH + "\\relatorio_ofs.txt";
      if(!System.IO.File.Exists(mensagem_caminho)) continue;
      var comunicado_mensagem = File.ReadAllText(mensagem_caminho);
      if(String.IsNullOrEmpty(comunicado_mensagem)) continue;
      Console.WriteLine($"< {DateTime.Now} Manager: Offensores do IDG");
      var usuarios = Database.recuperarUsuario(u =>
        u.has_privilege == UsersModel.userLevel.proprietario ||
        u.has_privilege == UsersModel.userLevel.administrador ||
        (u.has_privilege == UsersModel.userLevel.controlador && u.update_at.AddDays(cfg.DIAS_EXPIRACAO) > DateTime.Now) ||
        (u.has_privilege == UsersModel.userLevel.supervisor && u.update_at.AddDays(cfg.DIAS_EXPIRACAO * 3) > DateTime.Now)
      );
      ConsoleWrapper.Debug(Entidade.Advertiser, $"Usuários selecionados: {usuarios.Count()}");
      await Comunicado(usuarios, msg, cfg, cfg.ID_ADM_BOT, comunicado_mensagem, null, null, null);
      Console.WriteLine(comunicado_mensagem);
      System.IO.File.Delete(mensagem_caminho);
        }
      }
    }
  }
}