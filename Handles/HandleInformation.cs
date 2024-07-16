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
  async public static Task<Boolean> SendDocument(HandleMessage bot, Configuration cfg, UsersModel user, Request request)
  {
    var respostas = telbot.Temporary.executar(cfg, request.aplicacao, request.informacao);
    var verificacao = VerificarSAP(respostas);
    if(verificacao != null)
    {
      await bot.ErrorReport(user.id, new Exception(verificacao), request, verificacao);
      return false;
    }
    if(!Int32.TryParse(respostas.First(), out Int32 qnt))
    {
      await bot.ErrorReport(user.id, new Exception(verificacao), request, "A informação da quantidade de faturas não foi recebida!");
      return false;
    }
    respostas.Remove(respostas.First());
    List<String> faturas_validas = new();
    try
    {
      foreach (string fatura in respostas)
      {
        if (fatura == "None" || fatura == null || fatura == "") continue;
        if(!PdfChecker.PdfCheck($"./tmp/{fatura}", request.informacao)) continue;
        faturas_validas.Add(fatura);
      }
      if(faturas_validas.Count != qnt)
      {
        await bot.ErrorReport(user.id, new Exception(verificacao), request, "ERRO: A fatura recuperada não corresponde com a solicitada!");
        return false;
      }
      foreach (string fatura in faturas_validas)
      {
        await using Stream stream = System.IO.File.OpenRead($"./tmp/{fatura}");
        await bot.SendDocumentAsyncWraper(user.id, stream, fatura);
        stream.Dispose();
        await bot.sendTextMesssageWraper(user.id, fatura, false);
      }
      Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, true, request.received_at));
      return true;
    }
    catch (System.Exception error)
    {
      await bot.ErrorReport(id: user.id, request: request, error: error);
      return false;
    }
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
      {
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
          result = VerificarLockfile(cfg.OFS_LOCKFILE);
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
      }
      break;
      case "fatura":
      case "debito":
      {
        if(!cfg.PRL_SUBSISTEMA)
        {
          await bot.ErrorReport(user.id, new Exception(), request, "ERRO: O sistema monitor do PRL está desativado!");
          return;
        }
        var antes = DateTime.Now;
        var result = String.Empty;
        Updater.ClearTemp(cfg);
        System.IO.File.WriteAllText(cfg.PRL_LOCKFILE, $"{request.aplicacao} {request.informacao}", System.Text.Encoding.UTF8);
        while(true)
        {
          result = VerificarLockfile(cfg.PRL_LOCKFILE);
          if(!String.IsNullOrEmpty(result)) break;
          if((DateTime.Now - antes) >= TimeSpan.FromSeconds(cfg.ESPERA)) break;
          System.Threading.Thread.Sleep(10_000);
        }
        if(String.IsNullOrEmpty(result))
        {
          await bot.ErrorReport(user.id, new Exception(), request, "ERRO: Não foi recebida nenhuma resposta do PRL!");
          return;
        }
        var files = System.IO.Directory.GetFiles(cfg.TEMP_FOLDER);
        foreach (var file in files)
        {
          if(!PdfChecker.PdfCheck(file, request.informacao))
          {
            await bot.ErrorReport(user.id, new Exception(), request, "ERRO: A fatura recuperada não corresponde com a solicitada!");
            return;
          }
        }
        var fluxos = new Stream[files.Length];
        var tasks = new List<Task>();
        for(var i = 0; i < files.Length; i++)
        {
          var filename = System.IO.Path.GetFileName(files[i]);
          fluxos[i] = System.IO.File.OpenRead(files[i]);
          tasks.Add(bot.SendDocumentAsyncWraper(user.id, fluxos[i], filename));
        }
        await Task.WhenAll(tasks);
        foreach(var fluxo in fluxos) fluxo.Close();
        foreach(var file in files) File.Delete(file);
        await bot.sendTextMesssageWraper(user.id, result);
        Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, true, request.received_at));
      }
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
  public static String? VerificarLockfile(String lockfile)
  {
    var texto = System.IO.File.ReadAllText(lockfile, System.Text.Encoding.UTF8);
    return (texto.Length < 50) ? null : texto;
  }
}