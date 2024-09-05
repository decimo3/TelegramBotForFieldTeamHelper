using System.Data;
using telbot.Services;
using telbot.Helpers;
using telbot.models;
namespace telbot.handle;
public static class HandleAnnouncement
{
  public static async void Vencimento(String aplicacao, Int32 prazo)
  {
    ConsoleWrapper.Debug(Entidade.SoireeAsync, $"Monitor de {aplicacao} iniciado!");
    var cfg = Configuration.GetInstance();
    var contador_de_regionais = 0;
    while(true)
    {
      var tempo = cfg.IS_DEVELOPMENT ?  new TimeSpan(0, 5, 0) : new TimeSpan(1, 0, 0);
      if(cfg.REGIONAIS.Any()) tempo /= cfg.REGIONAIS.Count;
      await Task.Delay(tempo);
      if(DateTime.Now.DayOfWeek == DayOfWeek.Sunday) continue;
      if(DateTime.Now.Hour <= 7 || DateTime.Now.Hour >= 22) continue;
      var solicitacao = new logsModel() {
        identifier = contador_de_regionais,
        application = aplicacao,
        information = prazo,
        received_at = DateTime.Now.ToUniversalTime(),
        typeRequest = TypeRequest.xlsInfo
      };
      Database.GetInstance().InserirSolicitacao(solicitacao);
      contador_de_regionais = (contador_de_regionais + 1) % cfg.REGIONAIS.Count;
    }
  }
  public static async void Vencimento(String relatorio, logsModel solicitacao)
  {
    try
    {
      var cfg = Configuration.GetInstance();
      var msg = HandleMessage.GetInstance();
      var regional = cfg.REGIONAIS[(Int32)solicitacao.identifier];
      var relatorio_identificador = String.Empty;
      if(String.IsNullOrEmpty(relatorio))
      {
        var erro = new Exception(
          "Erro ao gerar o relatório de notas em aberto!");
        ConsoleWrapper.Error(Entidade.Advertiser, erro);
        return;
      }
      ConsoleWrapper.Write(Entidade.Advertiser,
        $"Comunicado de {solicitacao.application}, regional {regional}!");
      var filename = new String[] {
        solicitacao.response_at.ToString("u"),
        solicitacao.application,
        regional,
      };
      var relatorio_filename = String.Join('_', filename) + ".csv";
      var bytearray = System.Text.Encoding.UTF8.GetBytes(relatorio);
      using(var relatorio_arquivo = new MemoryStream(bytearray))
      {
        relatorio_identificador = await msg.SendDocumentAsyncWraper(
          cfg.ID_ADM_BOT,
          relatorio_arquivo,
          relatorio_filename
        );
      }
      if(String.IsNullOrEmpty(relatorio_identificador))
      {
          var erro = new Exception(
            "Erro ao gerar o relatório de notas em aberto!");
          ConsoleWrapper.Error(Entidade.Advertiser, erro);
          return;
      }
      var usuarios = Database.GetInstance().RecuperarUsuario(u => u.pode_relatorios());
      ConsoleWrapper.Debug(Entidade.Advertiser, $"Usuários selecionados: {usuarios.Count()}");
      await Comunicado(usuarios, cfg.ID_ADM_BOT, null, null, null, relatorio_identificador);
      return;
    }
    catch (System.Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Advertiser, erro);
      return;
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

    var usuarios = Database.GetInstance().RecuperarUsuario();

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
      if(usuario.dias_vencimento() < 0) continue;
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
  public static async void Executador(String imagename, String[] arguments, String[]? children)
  {
    var argumentos = new String[] {"/NH", "/FI", $"\"IMAGENAME eq {imagename}\""};
    while(true)
    {
      try
      {
        await Task.Delay(new TimeSpan(0, 1, 0));
        if(DateTime.Now.DayOfWeek == DayOfWeek.Saturday) continue;
        ConsoleWrapper.Debug(Entidade.Advertiser, $"Verificando se o sistema {imagename} está rodando...");
        var result = Executor.Executar("tasklist", argumentos, true);
        if(String.IsNullOrEmpty(result) || result.StartsWith("INFORMA"))
        {
          ConsoleWrapper.Debug(Entidade.Advertiser, $"Sistema {imagename} não está em execução. Iniciando...");
          if(children != null) Updater.Terminate(children);
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