namespace telbot.handle;
using telbot.Helpers;
using telbot.models;
using telbot.Services;
public static class Information
{
  async public static Task SendManuscripts(logsModel request)
  {
    var bot = HandleMessage.GetInstance();
    var argumentos = new String[] {request.application, request.information.ToString()};
    var respostas = Executor.Executar("sap.exe", argumentos, true);
    var verificacao = VerificarSAP(respostas);
    if(verificacao != null)
    {
      await bot.ErrorReport(request.identifier, new Exception(verificacao), request);
      return;
    }
    await bot.sendTextMesssageWraper(request.identifier, String.Join("\n", respostas));
    bot.SucessReport(request);
    return;
  }
  async public static Task SendCoordinates(logsModel request)
  {
    var bot = HandleMessage.GetInstance();
    var argumentos = new String[] {request.application, request.information.ToString()};
    var respostas = Executor.Executar("sap.exe", argumentos, true);
    var verificacao = VerificarSAP(respostas);
    if(verificacao != null)
    {
      await bot.ErrorReport(request.identifier, new Exception(verificacao), request);
      return;
    }
    await bot.SendCoordinateAsyncWraper(request.identifier, respostas[0]);
    bot.SucessReport(request);
    return;
  }
  async public static Task SendDocument(logsModel request)
  {
    var bot = HandleMessage.GetInstance();
    var argumentos = new String[] {request.application, request.information.ToString()};
    var respostas = Executor.Executar("sap.exe", argumentos, true);
    var verificacao = VerificarSAP(respostas);
    if(verificacao != null)
    {
      await bot.ErrorReport(request.identifier, new Exception(verificacao), request);
      return;
    }
    if(!Int32.TryParse(respostas.First(), out Int32 qnt))
    {
      var erro = new Exception("A informação da quantidade de faturas não foi recebida!");
      await bot.ErrorReport(request.identifier, erro, request);
      return;
    }
    respostas.Remove(respostas.First());
    List<String> faturas_validas = new();
    try
    {
      foreach (string fatura in respostas)
      {
        if (fatura == "None" || fatura == null || fatura == "") continue;
        if(!PdfChecker.PdfCheck($"./tmp/{fatura}", request.information)) continue;
        faturas_validas.Add(fatura);
      }
      if(faturas_validas.Count != qnt)
      {
        var erro = new Exception("ERRO: A fatura recuperada não corresponde com a solicitada!");
        await bot.ErrorReport(request.identifier, erro, request);
        return;
      }
      foreach (string fatura in faturas_validas)
      {
        await using Stream stream = System.IO.File.OpenRead($"./tmp/{fatura}");
        await bot.SendDocumentAsyncWraper(request.identifier, stream, fatura);
        stream.Dispose();
        await bot.sendTextMesssageWraper(request.identifier, fatura, false);
      }
      bot.SucessReport(request);
      return;
    }
    catch (System.Exception error)
    {
      await bot.ErrorReport(id: request.identifier, request: request, error: error);
      return;
    }
  }
  async public static Task SendPicture(logsModel request)
  {
    var bot = HandleMessage.GetInstance();
    var cfg = Configuration.GetInstance();
    var argumentos = new String[] {request.application, request.information.ToString()};
    var respostas = Executor.Executar("sap.exe", argumentos, true);
    var verificacao = VerificarSAP(respostas);
    if(verificacao != null)
    {
      var erro = new Exception(verificacao);
      await bot.ErrorReport(request.identifier, erro, request);
      return;
    }
    try
    {
      var csvarg = new String[] {String.Join('\n', respostas)};
      telbot.Executor.Executar("img.exe", csvarg, true);
      await using Stream stream = System.IO.File.OpenRead(@$"{cfg.CURRENT_PATH}\tmp\temporario.png");
      await bot.SendPhotoAsyncWraper(request.identifier, stream);
      stream.Dispose();
      System.IO.File.Delete(@$"{cfg.CURRENT_PATH}\tmp\temporario.png");
      if((request.application == "agrupamento") && (DateTime.Today.DayOfWeek == DayOfWeek.Friday))
        await bot.sendTextMesssageWraper(request.identifier, "*ATENÇÃO:* Não pode cortar agrupamento por nota de recorte!");
      await bot.sendTextMesssageWraper(request.identifier, $"Enviado relatorio de {request.application}!", false);
      bot.SucessReport(request);
    }
    catch (System.Exception error)
    {
      await bot.ErrorReport(id: request.identifier, request: request, error: error);
    }
    return;
  }
  async public static Task SendWorksheet(logsModel request)
  {
    var agora = DateTime.Now;
    var bot = HandleMessage.GetInstance();
    var cfg = Configuration.GetInstance();
    var argumentos = new String[] {request.application, request.information.ToString()};
    var respostas = Executor.Executar("sap.exe", argumentos, true);
    var verificacao = VerificarSAP(respostas);
    if(verificacao != null)
    {
      await bot.ErrorReport(request.identifier, new Exception(verificacao), request);
      return;
    }
    try
    {
      await using Stream stream = System.IO.File.OpenRead(cfg.TEMP_FOLDER + "/temporario.csv");
      await bot.SendDocumentAsyncWraper(request.identifier, stream, $"{agora.ToString("yyyyMMdd_HHmmss")}.csv");
      stream.Dispose();
      System.IO.File.Delete(cfg.TEMP_FOLDER + "/temporario.csv");
      await bot.sendTextMesssageWraper(request.identifier, $"Enviado arquivo de {request.application}: {agora.ToString("yyyyMMdd_HHmmss")}.XLSX", false);
      bot.SucessReport(request);
    }
    catch (System.Exception error)
    {
      await bot.ErrorReport(id: request.identifier, request: request, error: error);
    }
    return;
  }
  async public static Task SendMultiples(logsModel request)
  {
    var bot = HandleMessage.GetInstance();
    var cfg = Configuration.GetInstance();
    switch(request.application)
    {
      case "acesso":
        request.application = "coordenada";
        await SendCoordinates(request);
        request.application = "leiturista";
        await SendPicture(request);
        request.application = "cruzamento";
        await SendPicture(request);
      break;
      case "evidencia":
      {
        if(!cfg.OFS_MONITORAMENTO)
        {
          var erro = new Exception("ERRO: O sistema monitor do OFS está desativado!");
          await bot.ErrorReport(request.identifier, erro, request);
          return;
        }
        var antes = DateTime.Now;
        var result = String.Empty;
        Updater.ClearTemp(cfg);
        System.IO.File.WriteAllText(cfg.OFS_LOCKFILE, $"{request.application} {request.information}", System.Text.Encoding.UTF8);
        while(true)
        {
          result = VerificarLockfile(cfg.OFS_LOCKFILE);
          if(!String.IsNullOrEmpty(result)) break;
          if((DateTime.Now - antes) >= TimeSpan.FromSeconds(cfg.ESPERA)) break;
          System.Threading.Thread.Sleep(10_000);
        }
        if(String.IsNullOrEmpty(result))
        {
          var erro = new Exception("ERRO: Não foi recebida nenhuma resposta do OFS!");
          await bot.ErrorReport(request.identifier, erro, request);
          return;
        }
        await bot.sendTextMesssageWraper(request.identifier, result);
        var files = System.IO.Directory.GetFiles(cfg.TEMP_FOLDER);
        var fluxos = new Stream[files.Length];
        var tasks = new List<Task>();
        for(var i = 0; i < files.Length; i++)
        {
          var filename = System.IO.Path.GetFileName(files[i]);
          var fileext = System.IO.Path.GetExtension(files[i]);
          fluxos[i] = System.IO.File.OpenRead(files[i]);
          if(fileext == ".jpg")
            tasks.Add(bot.SendPhotoAsyncWraper(request.identifier, fluxos[i]));
          else if(fileext == ".jpeg")
            tasks.Add(bot.SendPhotoAsyncWraper(request.identifier, fluxos[i]));
          else
            tasks.Add(bot.SendDocumentAsyncWraper(request.identifier, fluxos[i], filename));
        }
        await Task.WhenAll(tasks);
        foreach(var fluxo in fluxos) fluxo.Close();
        foreach(var file in files) File.Delete(file);
        bot.SucessReport(request);
      }
      break;
      case "fatura":
      case "debito":
      {
        if(!cfg.PRL_SUBSISTEMA)
        {
          await bot.ErrorReport(request.identifier, new Exception("ERRO: O sistema monitor do PRL está desativado!"), request);
          return;
        }
        var antes = DateTime.Now;
        var result = String.Empty;
        Updater.ClearTemp(cfg);
        System.IO.File.WriteAllText(cfg.PRL_LOCKFILE, $"{request.application} {request.information}", System.Text.Encoding.UTF8);
        while(true)
        {
          result = VerificarLockfile(cfg.PRL_LOCKFILE);
          if(!String.IsNullOrEmpty(result)) break;
          if((DateTime.Now - antes) >= TimeSpan.FromSeconds(cfg.ESPERA)) break;
          System.Threading.Thread.Sleep(10_000);
        }
        if(String.IsNullOrEmpty(result))
        {
          var erro = new Exception("ERRO: Não foi recebida nenhuma resposta do PRL!");
          await bot.ErrorReport(request.identifier, erro, request);
          return;
        }
        var files = System.IO.Directory.GetFiles(cfg.TEMP_FOLDER);
        foreach (var file in files)
        {
          if(!PdfChecker.PdfCheck(file, request.information))
          {
            var erro = new Exception("ERRO: A fatura recuperada não corresponde com a solicitada!");
            await bot.ErrorReport(request.identifier, erro, request);
            return;
          }
        }
        var fluxos = new Stream[files.Length];
        var tasks = new List<Task>();
        for(var i = 0; i < files.Length; i++)
        {
          var filename = System.IO.Path.GetFileName(files[i]);
          fluxos[i] = System.IO.File.OpenRead(files[i]);
          tasks.Add(bot.SendDocumentAsyncWraper(request.identifier, fluxos[i], filename));
        }
        await Task.WhenAll(tasks);
        foreach(var fluxo in fluxos) fluxo.Close();
        foreach(var file in files) File.Delete(file);
        await bot.sendTextMesssageWraper(request.identifier, result);
        bot.SucessReport(request);
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
  async public static Task GetZoneInfo(Int64 id, Double latitude, Double longitude, DateTime received_at)
  {
    var bot = HandleMessage.GetInstance();
      var request = new logsModel() {
        identifier = id,
        application = "zona",
        received_at = received_at,
      };
    var argumentos = new String[] {
      latitude.ToString(System.Globalization.CultureInfo.InvariantCulture),
      longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)
    };
    var respostas = telbot.Executor.Executar("gps.exe", argumentos, true);
    var verificacao = VerificarSAP(respostas);
    if(verificacao != null)
    {
      await bot.ErrorReport(id, new Exception(verificacao), request);
      return;
    }
    await bot.sendTextMesssageWraper(id, String.Join("\n", respostas));
    bot.SucessReport(request);
    return;
  }
}