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
      tempo = cfg.IS_DEVELOPMENT ? new TimeSpan(0, 2, 30) : new TimeSpan(0, 30, 0);
      regional = cfg.REGIONAIS[contador_de_regionais];
    }
    Thread.Sleep(tempo);
    if(DateTime.Now.DayOfWeek == DayOfWeek.Sunday) continue;
    if(DateTime.Now.Hour <= 7 && DateTime.Now.Hour >= 22) continue;
    while(true)
    {
      if(!System.IO.File.Exists(cfg.LOCKFILE)) break;
      else System.Threading.Thread.Sleep(1_000);
    }
    try
    {
    ConsoleWrapper.Write(Entidade.Advertiser, $"Comunicado de {aplicacao} para todos!");
    System.IO.File.Create(cfg.LOCKFILE).Close();
    var relatorio_resultado = Temporary.executar(cfg, aplicacao, prazo, regional);
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
    relatorio_filename = new System.Text.RegularExpressions.Regex(padrao).Replace(relatorio_filename, "$3-$2-$1_$4-$5-$6");
    relatorio_filename = relatorio_arquivo != null ? $"{relatorio_filename}_{aplicacao}_{regional}.csv" : $"{relatorio_filename}_{aplicacao}.csv";
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
    System.IO.File.Delete(cfg.LOCKFILE);
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
    catch (System.Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Advertiser, erro);
    }
  }
  public static async void Monitorado(HandleMessage msg, Configuration cfg)
  {
    while(true)
    {
    try
    {
    var tempo = cfg.IS_DEVELOPMENT ? new TimeSpan(0, 0, 30) : new TimeSpan(0, 1, 0);
    System.Threading.Thread.Sleep(tempo);
    if(DateTime.Now.DayOfWeek == DayOfWeek.Saturday) continue;
    ConsoleWrapper.Debug(Entidade.Advertiser, "Verificando se o sistema de análise do OFS está rodando...");
    var result = Temporary.executar("tasklist", "/NH /FI \"IMAGENAME eq monitoring-fieldteam.exe\"", true);
    ConsoleWrapper.Debug(Entidade.Advertiser, String.Join(" ", result));
    if(result.First().StartsWith("INFORMA"))
    {
      ConsoleWrapper.Debug(Entidade.Advertiser, "Sistema não está em execução. Iniciando...");
      Updater.Terminate("ofs");
      Temporary.executar("monitoring-fieldteam.exe", "slower", false);
      continue;
    }
      ConsoleWrapper.Debug(Entidade.Advertiser, "Verificando relatórios de análise do OFS...");
      var mensagem_caminho = cfg.CURRENT_PATH + "\\relatorio_ofs.txt";
      if(!System.IO.File.Exists(mensagem_caminho)) 
      {
        ConsoleWrapper.Debug(Entidade.Advertiser, "Não foi encontrado relatórios do OFS.");
        continue;
      }
      var comunicado_linhas = File.ReadAllLines(mensagem_caminho);
      if(!comunicado_linhas.Any() || comunicado_linhas.Length < 2) 
      {
        ConsoleWrapper.Debug(Entidade.Advertiser, "O relatório do OFS é inválido.");
        System.IO.File.Delete(mensagem_caminho);
        continue;
      }
      var segunda_linha = comunicado_linhas[1];
      var i1 = segunda_linha.IndexOf('*') + 1;
      var i2 = segunda_linha.LastIndexOf('*');
      var balde_nome = segunda_linha[i1..i2];
      if(!cfg.BOT_CHANNELS.TryGetValue(balde_nome, out long channel))
        throw new InvalidOperationException("O balde encontrado não tem canal configurado!");
      var comunicado_mensagem = String.Join('\n', comunicado_linhas);
      ConsoleWrapper.Write(Entidade.Advertiser, "Comunicado de offensores do IDG:");
      await Comunicado(channel, msg, cfg, comunicado_mensagem, null, null, null);
      ConsoleWrapper.Write(Entidade.Advertiser, comunicado_mensagem);
      System.IO.File.Delete(mensagem_caminho);
    }
    catch (System.Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Advertiser, erro);
    }
    }
  }
  public static async void Finalizacao(HandleMessage msg, Configuration cfg)
  {
    while(true)
    {
      try
      {
        System.Threading.Thread.Sleep(new TimeSpan(1, 0, 0));
        var diretorio_ofs = cfg.CURRENT_PATH + @"\odl\";
        var lista_de_relatorios = System.IO.Directory.GetFiles(diretorio_ofs).Where(f => f.EndsWith(".done.csv")).ToList();
        foreach (var relatorio_filepath in lista_de_relatorios)
        {
          Stream relatorio_conteudo = System.IO.File.OpenRead(relatorio_filepath);
          if(relatorio_conteudo.Length == 0)
          {
            relatorio_conteudo.Close();
            continue;
          }
          var relatorio_filename = relatorio_filepath.Split('\\').Last();
          var relatorio_identificador = await msg.SendDocumentAsyncWraper(cfg.ID_ADM_BOT, relatorio_conteudo, relatorio_filename);
          relatorio_conteudo.Close();
          if(relatorio_identificador == String.Empty) return;

          var i1 = relatorio_filename.IndexOf('_') + 1;
          var i2 = relatorio_filename.IndexOf('.');
          var balde_nome = relatorio_filename[i1..i2];
          if(!cfg.BOT_CHANNELS.TryGetValue(balde_nome, out long channel))
            throw new InvalidOperationException("O balde encontrado não tem canal configurado!");

          await Comunicado(channel, msg, cfg, null, null, null, relatorio_identificador);
          ConsoleWrapper.Write(Entidade.Advertiser, $"Enviado relatorio final {relatorio_filename}!");
          System.IO.File.Move(relatorio_filepath, relatorio_filepath.Replace("done", "send"));
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
      if(text != null) tasks.Add(msg.sendTextMesssageWraper(canal, text, true, false));
      if(image_id != null) tasks.Add(msg.SendPhotoAsyncWraper(canal, image_id));
      if(video_id != null) tasks.Add(msg.SendVideoAsyncWraper(canal, video_id));
      if(doc_id != null) tasks.Add(msg.SendDocumentAsyncWraper(canal, doc_id));
      await Task.WhenAll(tasks);
    }
    catch (System.Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Advertiser, erro);
    }
  }
}