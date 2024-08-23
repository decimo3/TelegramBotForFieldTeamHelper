using telbot.Helpers;
using telbot.models;
namespace telbot.handle;
public static class HandleAsynchronous
{
  // DONE - Recebe, verifica e registra no banco de dados
  public static async Task Soiree(Int64 identificador, String mensagem, DateTime received_at)
  {
    var database = Database.GetInstance();
    var telegram = HandleMessage.GetInstance();
    ConsoleWrapper.Write(Entidade.Usuario, $"{identificador} escreveu: {mensagem}");
    var request = Validador.isRequest(mensagem);
    if (request is null)
    {
      await telegram.sendTextMesssageWraper(identificador, "Verifique o formato da informação e tente novamente da forma correta!");
      await telegram.sendTextMesssageWraper(identificador, "Se tiver em dúvida de como usar o bot, digite /ajuda.");
      return;
    }
    request.identifier = identificador;
    request.received_at = received_at.ToLocalTime();
    database.InserirSolicitacao(request);
  }
  // TODO - Coleta do banco de dados e realiza a solicitação
  public static async void Cooker(Int32 instance)
  {
    var cfg = Configuration.GetInstance();
    var database = Database.GetInstance();
    while (true)
    {
      var solicitacao = database.RecuperarSolicitacao(
        s => s.status == 0 && (s.rowid - instance) % cfg.SAP_INSTANCIA == 0
      ).FirstOrDefault();
      if (solicitacao == null) continue;
      switch (solicitacao.typeRequest)
      {
        case TypeRequest.gestao:
          {
            await Manager.HandleManager(solicitacao);
            continue;
          }
        case TypeRequest.comando:
          {
            await Command.HandleCommand(solicitacao);
            continue;
          }
        case TypeRequest.txtInfo:
        case TypeRequest.pdfInfo:
        case TypeRequest.picInfo:
        case TypeRequest.xlsInfo:
        case TypeRequest.xyzInfo:
          {
            var argumentos = new String[] {
            solicitacao.application,
            solicitacao.information.ToString(),
            "--instancia=" + instance,
            "--timestamp=" + solicitacao.received_at.ToString("U")
          };
            Executor.Executar("sap.exe", argumentos, false);
            break;
          }
        case TypeRequest.ofsInfo:
          {
            var antes = DateTime.Now;
            var argumentos = new String[] {
              solicitacao.application,
              solicitacao.information.ToString(),
              solicitacao.received_at.ToString("U")
            };
            while (true)
            {
              var texto = System.IO.File.ReadAllText("ofs.lock", System.Text.Encoding.UTF8);
              if (texto.Length > 0) continue;
              else System.IO.File.WriteAllText("ofs.lock", String.Join(' ', argumentos));
            }
          }
      }
      solicitacao.instance = instance;
      solicitacao.status = 300;
      database.AlterarSolicitacao(solicitacao);
    }
  }
  // TODO - Coleta resposta e responde ao usuário
  public static async void Waiter() { throw new NotImplementedException(); }
}