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
  public static async void Cooker() { throw new NotImplementedException(); }
  // TODO - Coleta resposta e responde ao usuário
  public static async void Waiter() { throw new NotImplementedException(); }
}