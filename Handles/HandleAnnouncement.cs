using System.Data;
using telbot.Helpers;
namespace telbot.handle;
public static class HandleAnnouncement
{
  private static int CINCO_MINUTOS = 1_000 * 60 * 5;
  public static async void Vencimento(HandleMessage msg, Configuration cfg, String aplicacao, Int32 prazo)
  {
    String? regional = null;
    var contador_de_regionais = 0;
    while(true)
    {
    var tempo = cfg.IS_DEVELOPMENT ?  new TimeSpan(0, 5, 0) : new TimeSpan(1, 0, 0);
    if(cfg.REGIONAIS.Any())
    {
      tempo = cfg.IS_DEVELOPMENT ? new TimeSpan(0, 5, 0) : new TimeSpan(0, 30, 0);
      regional = cfg.REGIONAIS[contador_de_regionais];
    }
    Thread.Sleep(tempo);
    if(DateTime.Now.DayOfWeek == DayOfWeek.Sunday) continue;
    if(DateTime.Now.Hour <= 7 || DateTime.Now.Hour >= 22) continue;
    while(true)
    {
      if(!System.IO.File.Exists(cfg.SAP_LOCKFILE)) break;
      else System.Threading.Thread.Sleep(1_000);
    }
    try
    {
    ConsoleWrapper.Write(Entidade.Advertiser, $"Comunicado de {aplicacao} para todos!");
    System.IO.File.Create(cfg.SAP_LOCKFILE).Close();
    var relatorio_resultado = Temporary.executar(cfg, aplicacao, prazo, regional: regional);
    var relatorio_caminho = cfg.CURRENT_PATH + "\\tmp\\temporario.csv";
    if(!relatorio_resultado.Any() || relatorio_resultado.First().StartsWith("ERRO:") || !System.IO.File.Exists(relatorio_caminho))
    {
      ConsoleWrapper.Error(Entidade.Advertiser, new Exception("Erro ao gerar o relatório de notas em aberto!\nTentaremos novamente daqui a cinco minutos"));
      System.IO.File.Delete(cfg.SAP_LOCKFILE);
      System.Threading.Thread.Sleep(CINCO_MINUTOS);
      continue;
    }
    Stream relatorio_arquivo = System.IO.File.OpenRead(relatorio_caminho);
    if(relatorio_arquivo.Length == 0)
    {
      relatorio_arquivo.Close();
      ConsoleWrapper.Error(Entidade.Advertiser, new Exception("Erro ao gerar o relatório de notas em aberto!\nTentaremos novamente daqui a cinco minutos"));
      System.IO.File.Delete(cfg.SAP_LOCKFILE);
      System.Threading.Thread.Sleep(CINCO_MINUTOS);
      continue;
    }
    var relatorio_mensagem = String.Join('\n', relatorio_resultado);
    var padrao = @"([0-9]{2})/([0-9]{2})/([0-9]{4}) ([0-9]{2}):([0-9]{2}):([0-9]{2})";
    var relatorio_filename = new System.Text.RegularExpressions.Regex(padrao).Match(relatorio_mensagem).Value;
    relatorio_filename = new System.Text.RegularExpressions.Regex(padrao).Replace(relatorio_filename, "$3-$2-$1_$4-$5-$6");
    relatorio_filename = relatorio_arquivo != null ? $"{relatorio_filename}_{aplicacao}_{regional}.csv" : $"{relatorio_filename}_{aplicacao}.csv";
    var relatorio_identificador = await msg.SendDocumentAsyncWraper(cfg.ID_ADM_BOT, relatorio_arquivo, relatorio_filename);
    if(relatorio_identificador == String.Empty)
    {
      relatorio_arquivo.Close();
      ConsoleWrapper.Error(Entidade.Advertiser, new Exception("Erro ao enviar o relatório de notas em aberto!\nTentaremos novamente daqui a cinco minutos"));
      System.IO.File.Delete(cfg.SAP_LOCKFILE);
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
    System.IO.File.Delete(cfg.SAP_LOCKFILE);
    contador_de_regionais = (contador_de_regionais + 1) % cfg.REGIONAIS.Count;
    }
    catch (System.Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Advertiser, erro);
    }
    }
  }
  public static async void Comunicado(HandleMessage msg, Configuration cfg)
  {
    try
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
    
    System.IO.File.Create(cfg.SAP_LOCKFILE).Close();
    
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
      ConsoleWrapper.Write(Entidade.Advertiser, comunicado_mensagem);
      System.IO.File.Delete(mensagem_caminho);
    }
    if(has_jpg)
    {
      ConsoleWrapper.Write(Entidade.Advertiser, "Enviada imagem do comunicado!");
      System.IO.File.Delete(imagem_caminho);
    }
    if(has_mp4)
    {
      ConsoleWrapper.Write(Entidade.Advertiser, "Enviado video do comunicado!");
      System.IO.File.Delete(videoclipe_caminho);
    }
    if(has_doc)
    {
      ConsoleWrapper.Write(Entidade.Advertiser, "Enviado documento do comunicado!");
      System.IO.File.Delete(documento_caminho);
    }
    System.IO.File.Delete(cfg.SAP_LOCKFILE);
    }
    catch (System.Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Advertiser, erro);
    }
  }
  public static async Task Comunicado(List<UsersModel> usuarios, HandleMessage msg, Configuration cfg, long myself, string? text, string? image_id, string? video_id, string? doc_id)
  {
    try
    {
    ConsoleWrapper.Write(Entidade.Advertiser, $"Comunicado para todos - Comunicado");
    var has_media = String.IsNullOrEmpty(image_id) || String.IsNullOrEmpty(video_id) || String.IsNullOrEmpty(doc_id);
    var tasks = new List<Task>();
    foreach (var usuario in usuarios)
    {
      if(usuario.id == myself) continue;
      if(image_id != null) tasks.Add(msg.SendPhotoAsyncWraper(usuario.id, image_id, text));
      if(video_id != null) tasks.Add(msg.SendVideoAsyncWraper(usuario.id, video_id, text));
      if(doc_id != null) tasks.Add(msg.SendDocumentAsyncWraper(usuario.id, doc_id, text));
      if(has_media == false && text != null) tasks.Add(msg.sendTextMesssageWraper(usuario.id, text, true, false));
    }
    await Task.WhenAll(tasks);
    }
    catch (System.Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Advertiser, erro);
    }
  }
  public static async void Executador(String imagename, String[] arguments, String[] children)
  {
    while(true)
    {
      try
      {
        await Task.Delay(new TimeSpan(0, 1, 0));
        System.Threading.Thread.Sleep(new TimeSpan(0, 1, 0));
        if(DateTime.Now.DayOfWeek == DayOfWeek.Saturday) continue;
        ConsoleWrapper.Debug(Entidade.Advertiser, $"Verificando se o sistema {imagename} está rodando...");
        var result = Temporary.executar("tasklist", $"/NH /FI \"IMAGENAME eq {imagename}\"", true);
        ConsoleWrapper.Debug(Entidade.Advertiser, String.Join('\n', result));
        if(result.First().StartsWith("INFORMA"))
        {
          ConsoleWrapper.Debug(Entidade.Advertiser, $"Sistema {imagename} não está em execução. Iniciando...");
          Updater.Terminate(children);
          Temporary.executar(imagename, String.Join(' ', arguments), false);
        }
      }
      catch (System.Exception erro)
      {
        ConsoleWrapper.Error(Entidade.Advertiser, erro);
      }
    }
  }
  public static async Task Comunicado(Int64 canal, HandleMessage msg, Configuration cfg, string? text, string? image_id, string? video_id, string? doc_id)
  {
    try
    {
      var tasks = new List<Task>();
      var has_media = String.IsNullOrEmpty(image_id) || String.IsNullOrEmpty(video_id) || String.IsNullOrEmpty(doc_id);
      if(image_id != null) tasks.Add(msg.SendPhotoAsyncWraper(canal, image_id, text));
      if(video_id != null) tasks.Add(msg.SendVideoAsyncWraper(canal, video_id, text));
      if(doc_id != null) tasks.Add(msg.SendDocumentAsyncWraper(canal, doc_id, text));
      if(has_media == false && text != null) tasks.Add(msg.sendTextMesssageWraper(canal, text, true, false));
      await Task.WhenAll(tasks);
    }
    catch (System.Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Advertiser, erro);
    }
  }
}