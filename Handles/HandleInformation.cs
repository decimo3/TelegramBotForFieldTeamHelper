namespace telbot.handle;
using telbot.Helpers;
using telbot.models;
public static class Information
{
  async public static Task SendManuscripts(HandleMessage bot, Configuration cfg, UsersModel user, Request request)
  {
    var respostas = telbot.Temporary.executar(cfg, request.aplicacao!, request.informacao!, telefone: user.phone_number);
    var verificacao = VerificarSAP(respostas);
    if(verificacao != null)
    {
      await bot.ErrorReport(user.id, new Exception(verificacao), request, verificacao);
      return;
    }
    await bot.sendTextMesssageWraper(user.id, String.Join("\n", respostas));
    Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, true, request.received_at));
    return;
  }
  async public static Task SendCoordinates(HandleMessage bot, Configuration cfg, UsersModel user, Request request)
  {
    var respostas = telbot.Temporary.executar(cfg, request.aplicacao!, request.informacao!);
    var verificacao = VerificarSAP(respostas);
    if(verificacao != null)
    {
      await bot.ErrorReport(user.id, new Exception(verificacao), request, verificacao);
      return;
    }
    await bot.SendCoordinateAsyncWraper(user.id, respostas[0]);
    Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, true, request.received_at));
    return;
  }
  async public static Task SendDocument(HandleMessage bot, Configuration cfg, UsersModel user, Request request)
  {
    var respostas = telbot.Temporary.executar(cfg, request.aplicacao, request.informacao);
    var verificacao = VerificarSAP(respostas);
    if(verificacao != null)
    {
      await bot.ErrorReport(user.id, new Exception(verificacao), request, verificacao);
      return;
    }
    try
    {
      foreach (string fatura in respostas)
      {
        if (fatura == "None" || fatura == null || fatura == "") continue;
        if(!PdfChecker.PdfCheck($"./tmp/{fatura}", request.informacao))
          throw new InvalidOperationException("ERRO: A fatura recuperada não corresponde com a solicitada!");
      }
      foreach (string fatura in respostas)
      {
        if (fatura == "None" || fatura == null || fatura == "") continue;
        await using Stream stream = System.IO.File.OpenRead($"./tmp/{fatura}");
        await bot.SendDocumentAsyncWraper(user.id, stream, fatura);
        stream.Dispose();
        await bot.sendTextMesssageWraper(user.id, fatura, false);
      }
      Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, true, request.received_at));
    }
    catch (System.Exception error)
    {
      await bot.ErrorReport(id: user.id, request: request, error: error);
    }
    return;
  }
  async public static Task SendPicture(HandleMessage bot, Configuration cfg, UsersModel user, Request request)
  {
    var respostas = telbot.Temporary.executar(cfg, request.aplicacao!, request.informacao!);
    var verificacao = VerificarSAP(respostas);
    if(verificacao != null)
    {
      await bot.ErrorReport(user.id, new Exception(verificacao), request, verificacao);
      return;
    }
    try
    {
      telbot.Temporary.executar(cfg, respostas);
      await using Stream stream = System.IO.File.OpenRead(@$"{cfg.CURRENT_PATH}\tmp\temporario.png");
      await bot.SendPhotoAsyncWraper(user.id, stream);
      stream.Dispose();
      System.IO.File.Delete(@$"{cfg.CURRENT_PATH}\tmp\temporario.png");
      Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, true, request.received_at));
      if((request.aplicacao == "agrupamento") && (DateTime.Today.DayOfWeek == DayOfWeek.Friday))
      await bot.sendTextMesssageWraper(user.id, "*ATENÇÃO:* Não pode cortar agrupamento por nota de recorte!");
      await bot.sendTextMesssageWraper(user.id, $"Enviado relatorio de {request.aplicacao}!", false);
    }
    catch (System.Exception error)
    {
      await bot.ErrorReport(id: user.id, request: request, error: error);
    }
    return;
  }
  async public static Task SendWorksheet(HandleMessage bot, Configuration cfg, UsersModel user, Request request)
  {
    var agora = DateTime.Now;
    var respostas = telbot.Temporary.executar(cfg, request.aplicacao!, request.informacao!);
    var verificacao = VerificarSAP(respostas);
    if(verificacao != null)
    {
      await bot.ErrorReport(user.id, new Exception(verificacao), request, verificacao);
      return;
    }
    try
    {
      await using Stream stream = System.IO.File.OpenRead(cfg.TEMP_FOLDER + "/temporario.csv");
      await bot.SendDocumentAsyncWraper(user.id, stream, $"{agora.ToString("yyyyMMdd_HHmmss")}.csv");
      stream.Dispose();
      System.IO.File.Delete(cfg.TEMP_FOLDER + "/temporario.csv");
      await bot.sendTextMesssageWraper(user.id, $"Enviado arquivo de {request.aplicacao}: {agora.ToString("yyyyMMdd_HHmmss")}.XLSX", false);
      Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, true, request.received_at));
    }
    catch (System.Exception error)
    {
      await bot.ErrorReport(id: user.id, request: request, error: error);
    }
    return;
  }
  async public static Task SendMultiples(HandleMessage bot, Configuration cfg, UsersModel user, Request request)
  {
    switch(request.aplicacao)
    {
      case "acesso":
        request.aplicacao = "coordenada";
        await SendCoordinates(bot, cfg, user, request);
        request.aplicacao = "leiturista";
        await SendPicture(bot, cfg, user, request);
        request.aplicacao = "cruzamento";
        await SendPicture(bot, cfg, user, request);
      break;
      case "evidencia":
        if(!cfg.OFS_MONITORAMENTO)
        {
          await bot.ErrorReport(user.id, new Exception(), request, "ERRO: O sistema monitor do OFS está desativado!");
          return;
        }
        var antes = DateTime.Now;
        var result = String.Empty;
        Updater.ClearTemp(cfg);
        System.IO.File.WriteAllText(cfg.OFS_LOCKFILE, $"{request.aplicacao} {request.informacao}", System.Text.Encoding.UTF8);
        while(true)
        {
          result = VerificarOFS(cfg);
          if(!String.IsNullOrEmpty(result)) break;
          if((DateTime.Now - antes) >= TimeSpan.FromSeconds(cfg.ESPERA)) break;
          System.Threading.Thread.Sleep(10_000);
        }
        if(String.IsNullOrEmpty(result))
        {
          await bot.ErrorReport(user.id, new Exception(), request, "ERRO: Não foi recebida nenhuma resposta do OFS!");
          return;
        }
        await bot.sendTextMesssageWraper(user.id, result);
        var files = System.IO.Directory.GetFiles(cfg.TEMP_FOLDER);
        var fluxos = new Stream[files.Length];
        var tasks = new List<Task>();
        for(var i = 0; i < files.Length; i++)
        {
          var filename = System.IO.Path.GetFileName(files[i]);
          var fileext = System.IO.Path.GetExtension(files[i]);
          fluxos[i] = System.IO.File.OpenRead(files[i]);
          if(fileext == ".jpg")
            tasks.Add(bot.SendPhotoAsyncWraper(user.id, fluxos[i]));
          else if(fileext == ".jpeg")
            tasks.Add(bot.SendPhotoAsyncWraper(user.id, fluxos[i]));
          else
            tasks.Add(bot.SendDocumentAsyncWraper(user.id, fluxos[i], filename));
        }
        await Task.WhenAll(tasks);
        foreach(var fluxo in fluxos) fluxo.Close();
        foreach(var file in files) File.Delete(file);
        Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, true, request.received_at));
      break;
    }
    return;
  }
  public static String? VerificarSAP(List<String> respostas)
  {
    if(!respostas.Any()) return "ERRO: Não foi recebida nenhuma resposta do SAP";
    if(respostas.First().StartsWith("ERRO")) return String.Join("\n", respostas);
    return null;
  }
  public static String? VerificarOFS(Configuration cfg)
  {
    var texto = System.IO.File.ReadAllText(cfg.OFS_LOCKFILE, System.Text.Encoding.UTF8);
    return (texto.Length < 50) ? null : texto;
  }
}