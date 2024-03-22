namespace telbot.handle;
using telbot.models;
public static class Information
{
  async public static Task SendManuscripts(HandleMessage bot, Configuration cfg, UsersModel user, Request request)
  {
    var respostas = telbot.Temporary.executar(cfg, request.aplicacao!, request.informacao!);
    var verificacao = VerificarSAP(respostas);
    if(verificacao != null)
    {
      await bot.ErrorReport(user.id, new Exception(verificacao), request, verificacao);
      return;
    }
    string textoMensagem = String.Empty;
    foreach (var resposta in respostas)
    {
      textoMensagem += resposta;
      textoMensagem += "\n";
    }
    await bot.sendTextMesssageWraper(user.id, textoMensagem);
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
      await using Stream stream = System.IO.File.OpenRead(@"C:\Users\ruan.camello\SapWorkDir\export.XLSX");
      await bot.SendDocumentAsyncWraper(user.id, stream, $"{agora.ToString("yyyyMMdd_HHmmss")}.XLSX");
      stream.Dispose();
      System.IO.File.Delete(@"C:\Users\ruan.camello\SapWorkDir\export.XLSX");
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
    }
    return;
  }
  public static String? VerificarSAP(List<String> respostas)
  {
    if(respostas.Count == 0) return "ERRO: Não foi recebida nenhuma resposta do SAP";
    if(respostas[0].StartsWith("ERRO")) return respostas.First();
    return null;
  }
}