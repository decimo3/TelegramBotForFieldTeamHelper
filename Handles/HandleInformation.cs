namespace telbot.handle;
using telbot.models;
public class HandleInformation
{
  private HandleMessage bot;
  private Request request;
  private UsersModel user;
  private Configuration cfg;
  private List<string> respostas;
  private DateTime agora;
  public HandleInformation(HandleMessage bot, Configuration cfg, UsersModel user, Request request)
  {
    this.bot = bot;
    this.cfg = cfg;
    this.user = user;
    this.request = request;
    this.agora = DateTime.Now;
    this.respostas = new();
  }
  async private Task<bool> has_impediment()
  {
    if(request.aplicacao == "passivo" && (DateTime.Today.DayOfWeek == DayOfWeek.Friday || DateTime.Today.DayOfWeek == DayOfWeek.Saturday))
    {
      await bot.sendTextMesssageWraper(user.id, "Essa aplicação não deve ser usada na sexta e no sábado!");
      await bot.sendTextMesssageWraper(user.id, "Notas de recorte devem ter todas as faturas cobradas!");
      Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, false, request.received_at));
      return true;
    }
    // Knockout system to mitigate the queue
    if((request.tipo == TypeRequest.pdfInfo) && (cfg.GERAR_FATURAS == true))
    {
      var knockout = DateTime.Now.AddMinutes(-3);
      if(System.DateTime.Compare(knockout, request.received_at) > 0)
      {
          await bot.sendTextMesssageWraper(user.id, "Devido a fila de solicitações, estaremos te enviando as informações do cliente!");
          request.aplicacao = "informacao";
          return false;
      }
      return false;
    }
    if((request.tipo == TypeRequest.pdfInfo) && (cfg.GERAR_FATURAS == false))
    {
      await bot.sendTextMesssageWraper(user.id, "O sistema SAP não está gerando faturas no momento!\nEstaremos te enviando as informações do cliente!");
      request.aplicacao = "informacao";
      return false;
    }
    if((request.tipo == TypeRequest.xlsInfo) && (user.has_privilege == false))
    {
      await bot.sendTextMesssageWraper(user.id, "Você não tem permissão para gerar relatórios!");
      Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, false, request.received_at));
      return true;
    }
    return false;
  }
  async public Task routeInformation()
  {
    if(await has_impediment()) return;
    if (this.request.tipo == TypeRequest.anyInfo)
    {
      await this.SendMultiples();
      return;
    }
    await this.ExecuteSAP();
    switch (this.request.aplicacao)
    {
      case "telefone":await SendManuscripts(); break;
      case "coordenada":await SendCoordinates(); break;
      case "localizacao":await SendManuscripts(); break;
      case "leiturista":await SendPicture(); break;
      case "roteiro":await SendPicture(); break;
      case "fatura":await SendDocument(); break;
      case "debito":await SendDocument(); break;
      case "historico":await SendPicture(); break;
      case "contato":await SendManuscripts(); break;
      case "agrupamento":await SendPicture(); break;
      case "pendente":await SendPicture(); break;
      case "relatorio":await SendWorksheet(); break;
      case "manobra":await SendWorksheet(); break;
      case "medidor":await SendManuscripts(); break;
      case "passivo":await SendDocument(); break;
      case "suspenso":await SendManuscripts(); break;
      case "informacao":await SendManuscripts(); break;
      case "cruzamento":await SendPicture(); break;
      case "consumo":await SendPicture(); break;
    }
    return;
  }
  // Para envio de texto simples
  async public Task SendManuscripts()
  {
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
  async public Task SendCoordinates()
  {
    await bot.SendCoordinateAsyncWraper(user.id, respostas[0]);
    Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, true, request.received_at));
    return;
  }
  // Para envio de faturas em PDF
  async public Task SendDocument()
  {
    try
    {
      foreach (string fatura in respostas)
      {
        if (fatura == "None" || fatura == null || fatura == "") continue;
        if(!PdfChecker.PdfCheck($"./tmp/{fatura}", request.informacao!))
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
  // Para envio de relatórios
  async public Task SendPicture()
  {
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
  // Para envio de planilhas
  async public Task SendWorksheet()
  {
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
  async public Task SendMultiples()
  {
    switch(this.request.aplicacao)
    {
      case "acesso":
        this.request.aplicacao = "coordenada";
        await this.ExecuteSAP();
        await SendCoordinates();
        this.request.aplicacao = "leiturista";
        await this.ExecuteSAP();
        await SendPicture();
        this.request.aplicacao = "cruzamento";
        await this.ExecuteSAP();
        await SendPicture();
      break;
    }
    return;
  }
  async public Task ExecuteSAP()
  {
    respostas = telbot.Temporary.executar(cfg, this.request.aplicacao!, this.request.informacao!);
    if(respostas.Count == 0)
    {
      var erro = new Exception("Erro no script do SAP");
      await bot.ErrorReport(id: user.id, request: request, error: erro);
      return;
    }
    if(respostas[0].StartsWith("ERRO"))
    {
      var erro = new Exception("Erro no script do SAP");
      await bot.ErrorReport(id: user.id, error: erro, request: request, respostas[0]);
      return;
    }
    return;
  }
}