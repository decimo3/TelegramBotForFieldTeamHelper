using System.Data;
using telbot.Services;
using telbot.Helpers;
namespace telbot.handle;
public static class HandleAnnouncement
{
  public static async void Vencimento(String aplicacao, Int32 prazo)
  {
    var CINCO_MINUTOS = new TimeSpan(0, 5, 0);
    var cfg = Configuration.GetInstance();
    var msg = HandleMessage.GetInstance();
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
    await Task.Delay(tempo);
    if(DateTime.Now.DayOfWeek == DayOfWeek.Sunday) continue;
    if(DateTime.Now.Hour <= 7 || DateTime.Now.Hour >= 22) continue;
    while(true)
    {
      if(!System.IO.File.Exists("sap.lock")) break;
      else await Task.Delay(cfg.TASK_DELAY);
    }
    try
    {
    ConsoleWrapper.Write(Entidade.Advertiser, $"Comunicado de {aplicacao} para todos!");
    System.IO.File.Create("sap.lock").Close();
    // TODO - Resolver conflito de execução com as solicitações
    // TODO - Substituir todas as chamadas para uso do lockfile
    var argumentos = new String[] {aplicacao, prazo.ToString(), "--regional=" + regional};
    var relatorio_resultado = Executor.Executar("sap.exe", argumentos, true);
    var relatorio_caminho = cfg.CURRENT_PATH + "\\tmp\\temporario.csv";
    if(!relatorio_resultado.Any() || relatorio_resultado.First().StartsWith("ERRO:") || !System.IO.File.Exists(relatorio_caminho))
    {
      ConsoleWrapper.Error(Entidade.Advertiser, new Exception("Erro ao gerar o relatório de notas em aberto!\nTentaremos novamente daqui a cinco minutos"));
      System.IO.File.Delete("sap.lock");
      await Task.Delay(CINCO_MINUTOS);
      continue;
    }
    Stream relatorio_arquivo = System.IO.File.OpenRead(relatorio_caminho);
    if(relatorio_arquivo == null || relatorio_arquivo.Length == 0)
    {
      if(relatorio_arquivo != null) relatorio_arquivo.Close();
      ConsoleWrapper.Error(Entidade.Advertiser, new Exception("Erro ao gerar o relatório de notas em aberto!\nTentaremos novamente daqui a cinco minutos"));
      System.IO.File.Delete("sap.lock");
      await Task.Delay(CINCO_MINUTOS);
      continue;
    }
    var relatorio_mensagem = String.Join('\n', relatorio_resultado);
    var padrao = @"([0-9]{2})/([0-9]{2})/([0-9]{4}) ([0-9]{2}):([0-9]{2}):([0-9]{2})";
    var relatorio_filename = new System.Text.RegularExpressions.Regex(padrao).Match(relatorio_mensagem).Value;
    relatorio_filename = new System.Text.RegularExpressions.Regex(padrao).Replace(relatorio_filename, "$3-$2-$1_$4-$5-$6");
    relatorio_filename = regional != null ? $"{relatorio_filename}_{aplicacao}_{regional}.csv" : $"{relatorio_filename}_{aplicacao}.csv";
    var relatorio_identificador = await msg.SendDocumentAsyncWraper(cfg.ID_ADM_BOT, relatorio_arquivo, relatorio_filename);
    if(relatorio_identificador == String.Empty)
    {
      relatorio_arquivo.Close();
      ConsoleWrapper.Error(Entidade.Advertiser, new Exception("Erro ao enviar o relatório de notas em aberto!\nTentaremos novamente daqui a cinco minutos"));
      System.IO.File.Delete("sap.lock");
      await Task.Delay(CINCO_MINUTOS);
      continue;
    }
    var usuarios = Database.GetInstance().RecuperarUsuario(u => u.dias_vencimento() > 0);
    ConsoleWrapper.Debug(Entidade.Advertiser, $"Usuários selecionados: {usuarios.Count()}");
    await Comunicado(usuarios, cfg.ID_ADM_BOT, relatorio_mensagem, null, null, relatorio_identificador);
    relatorio_arquivo.Close();
    System.IO.File.Delete(relatorio_caminho);
    System.IO.File.Delete("sap.lock");
    contador_de_regionais = (contador_de_regionais + 1) % cfg.REGIONAIS.Count;
    }
    catch (System.Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Advertiser, erro);
    }
    }
  }
  public static async void Comunicado()
  {
    var cfg = Configuration.GetInstance();
    var msg = HandleMessage.GetInstance();
    var mensagem_caminho = cfg.CURRENT_PATH + "\\comunicado.txt";
    var imagem_caminho = cfg.CURRENT_PATH + "\\comunicado.jpg";
    var videoclipe_caminho = cfg.CURRENT_PATH + "\\comunicado.mp4";
    var documento_caminho = cfg.CURRENT_PATH + "\\comunicado.pdf";
    try
    {

    var has_txt = System.IO.File.Exists(mensagem_caminho);
    var has_jpg = System.IO.File.Exists(imagem_caminho);
    var has_mp4 = System.IO.File.Exists(videoclipe_caminho);
    var has_doc = System.IO.File.Exists(documento_caminho);
    
    if(!has_txt && !has_jpg && !has_mp4 && !has_doc) return;
    
    System.IO.File.Create("sap.lock").Close();
    
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

    var usuarios = Database.GetInstance().RecuperarUsuario(u => u.dias_vencimento() > 0);

    ConsoleWrapper.Debug(Entidade.Advertiser, $"Usuários selecionados: {usuarios.Count()}");
    await Comunicado(usuarios, cfg.ID_ADM_BOT, comunicado_mensagem, photo_id, video_id, doc_id);

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
    System.IO.File.Delete("sap.lock");
    }
    catch (System.Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Advertiser, erro);
    }
  }
  public static async Task Comunicado(List<UsersModel> usuarios, long myself, string? text, string? image_id, string? video_id, string? doc_id)
  {
    var msg = HandleMessage.GetInstance();
    try
    {
    ConsoleWrapper.Write(Entidade.Advertiser, $"Comunicado para todos - Comunicado");
    var has_media = String.IsNullOrEmpty(image_id) || String.IsNullOrEmpty(video_id) || String.IsNullOrEmpty(doc_id);
    var tasks = new List<Task>();
    foreach (var usuario in usuarios)
    {
      if(usuario.identifier == myself) continue;
      if(image_id != null) tasks.Add(msg.SendPhotoAsyncWraper(usuario.identifier, image_id, text));
      if(video_id != null) tasks.Add(msg.SendVideoAsyncWraper(usuario.identifier, video_id, text));
      if(doc_id != null) tasks.Add(msg.SendDocumentAsyncWraper(usuario.identifier, doc_id, text));
      if(has_media == false && text != null) tasks.Add(msg.sendTextMesssageWraper(usuario.identifier, text, true, false));
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
    var argumentos = new String[] {"/NH", "/FI", "\"IMAGENAME eq {imagename}\""};
    while(true)
    {
      try
      {
        await Task.Delay(new TimeSpan(0, 1, 0));
        if(DateTime.Now.DayOfWeek == DayOfWeek.Saturday) continue;
        ConsoleWrapper.Debug(Entidade.Advertiser, $"Verificando se o sistema {imagename} está rodando...");
        var result = Executor.Executar("tasklist", argumentos, true);
        if(result.First().StartsWith("INFORMA"))
        {
          ConsoleWrapper.Debug(Entidade.Advertiser, $"Sistema {imagename} não está em execução. Iniciando...");
          Updater.Terminate(children);
          Executor.Executar(imagename, arguments, false);
        }
      }
      catch (System.Exception erro)
      {
        ConsoleWrapper.Error(Entidade.Advertiser, erro);
      }
    }
  }
  public static async Task Comunicado(Int64 canal, string? text, string? image_id, string? video_id, string? doc_id)
  {
    var msg = HandleMessage.GetInstance();
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