using telbot.Helpers;
namespace telbot.handle;
public static class Recovery
{
  public static void ErrorSendMessageReport(errorReport report)
  {
    Database.InserirPerdido(report);
    ConsoleWrapper.Error(Entidade.Recovery, new Exception($"Não foi possível enviar a mensagem {report.mensagem} ao usuario"));
  }
  public async static void ErrorSendMessageRecovery(HandleMessage msg)
  {
    while (true)
    {
    try
    {
    System.Threading.Thread.Sleep(60_000);
    ConsoleWrapper.Debug(Entidade.Recovery, "Verificando se há mensagens perdidas...");
    var perdidos = Database.RecuperarPerdido();
    if(!perdidos.Any())
    {
      ConsoleWrapper.Debug(Entidade.Recovery, "Não foram encontradas mensagens perdidas!");
      return;
    }
    var tasks = new List<Task>();
    foreach (var perdido in perdidos)
    {
      if(perdido.binario.Length == 0)
      {
        if(perdido.mensagem == null)
        {
          tasks.Add(msg.RequestContact(perdido.identificador));
          continue;
        }
        if(perdido.mensagem.StartsWith("+"))
        {
          tasks.Add(msg.RemoveRequest(perdido.identificador, perdido.mensagem));
          continue;
        }
        if(perdido.mensagem.StartsWith("-"))
        {
          tasks.Add(msg.SendCoordinateAsyncWraper(perdido.identificador, perdido.mensagem));
          continue;
        }
        tasks.Add(msg.sendTextMesssageWraper(perdido.identificador, perdido.mensagem));
      }
      else
      {
        if(perdido.mensagem == null)
        {
          tasks.Add(msg.SendPhotoAsyncWraper(perdido.identificador, perdido.binario));
        }
        else
        {
          tasks.Add(msg.SendDocumentAsyncWraper(perdido.identificador, perdido.binario, perdido.mensagem));
        }
      }
    }
    await Task.WhenAll(tasks);
    Database.ExcluirPerdidos();
    }
    catch (System.Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Recovery, erro);
    }
    }
  }
}
