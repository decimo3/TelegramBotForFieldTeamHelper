using telbot.Helpers;
using telbot.models;
namespace telbot.handle;
public static class HandleAsynchronous
{
  // TODO - Recebe, verifica e registra no banco de dados
  public static async Task Soiree(RequestText solicitacao)
  {
    var database = Database.GetInstance();
    var telegram = HandleMessage.GetInstance();
    ConsoleWrapper.Write(Entidade.Usuario, $"{solicitacao.Identificador} escreveu: {solicitacao.Mensagem}");
    var request = Validador.isRequest(solicitacao.Mensagem);
    if (request is null)
    {
      await telegram.sendTextMesssageWraper(solicitacao.Identificador, "Verifique o formato da informação e tente novamente da forma correta!");
      await telegram.sendTextMesssageWraper(solicitacao.Identificador, "Se tiver em dúvida de como usar o bot, digite /ajuda.");
      return;
    }
    request.received_at = solicitacao.RecebidoEm;
    request.identifier = solicitacao.Identificador;
    database.InserirSolicitacao(request);
  }
  // TODO - Coleta do banco de dados e realiza a solicitação
  public static async void Cooker() { throw new NotImplementedException(); }
  // TODO - Coleta resposta e responde ao usuário
  public static async void Waiter() { throw new NotImplementedException(); }
}