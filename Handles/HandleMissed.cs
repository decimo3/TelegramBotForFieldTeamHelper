namespace telbot.handle;
public static class Recovery
{
  public static void ErrorSendMessageReport(errorReport report)
  {
    Database.InserirPerdido(report);
    Console.BackgroundColor = ConsoleColor.Red;
    Console.Beep();
    Console.WriteLine($"< {DateTime.Now} chatbot: Não foi possível enviar mensagem ao usuario");
    Console.BackgroundColor = ConsoleColor.Black;
  }
  public async static Task ErrorSendMessageRecovery(HandleMessage msg)
  {
    var tasks = new List<Task>();
    var perdidos = Database.RecuperarPerdido();
    if(!perdidos.Any()) return;
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
}
