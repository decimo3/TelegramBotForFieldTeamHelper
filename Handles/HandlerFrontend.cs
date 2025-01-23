namespace telbot.handle;
public class FrontEnd
{
  private readonly Int32 X_MAX = Console.WindowWidth - 2;
  private readonly Int32 Y_MAX = Console.WindowHeight;
  private readonly String[] HEADERS =
  {
    "rowid", "identifier", "application", "information", "typeRequest",
    "received_at", "response_at", "instance", "status"
  };
  private Int32 X_CUR = 0;
  private Int32 Y_CUR = 0;
  private readonly Int32[] TAMANHOS = 
  {
    6, 16, 16, 16, 12, 20, 20, 10, 6
  };
  private enum DESTAQUES {AUSENTE = 0, VERDE = 200, AMARELO = 300, VERMELHO = 500}
  private void DrawLine(Object[] textos, DESTAQUES destaque = DESTAQUES.AUSENTE)
  {
    if (Y_CUR >= Y_MAX) return;
    for (int i = 0; i < textos.Length; i++)
    {
      Console.SetCursorPosition(X_CUR, Y_CUR);
      switch (destaque)
      {
        case DESTAQUES.AUSENTE:
          Console.BackgroundColor = ConsoleColor.Black;
          Console.ForegroundColor = ConsoleColor.White;
        break;
        case DESTAQUES.VERDE:
          Console.BackgroundColor = ConsoleColor.Black;
          Console.ForegroundColor = ConsoleColor.Green;
        break;
        case DESTAQUES.AMARELO:
          Console.BackgroundColor = ConsoleColor.Black;
          Console.ForegroundColor = ConsoleColor.Yellow;
        break;
        case DESTAQUES.VERMELHO:
          Console.BackgroundColor = ConsoleColor.Black;
          Console.ForegroundColor = ConsoleColor.Red;
        break;
      }
      Console.Write('|');
      Console.Write(textos[i].ToString());
      X_CUR += TAMANHOS[i] + 1;
    }
    Console.SetCursorPosition(X_MAX, Y_CUR);
    Console.Write('|');
    X_CUR = 0;
    Y_CUR += 1;
  }
  public async void Start()
  {
    // Hide the cursor
    Console.CursorVisible = false;
    while(true)
    {
      // Get requests
      var requests = HandleQueQue.GetInstance().Get(Y_MAX);
      // Clear the console
      Console.Clear();
      // Draw table header  
      DrawLine(new string[]{new String('-', X_MAX)});
      DrawLine(HEADERS);
      DrawLine(new string[]{new String('-', X_MAX)});
      // Draw line data
      requests = requests.OrderByDescending(s => s.received_at).ToList();
      foreach (var request in requests)
      {
        var linha = new Object[]
        {
          request.rowid,
          request.identifier,
          request.application,
          request.information,
          request.typeRequest,
          request.received_at,
          request.response_at,
          request.instance,
          request.status
        };
        DrawLine(linha.Take(Y_MAX).ToArray(), (DESTAQUES)request.status);
      }
      await Task.Delay(1_000);
    }
  }
}