using telbot.models;
namespace telbot.handle;
public static class FrontEnd
{
  private static readonly String[] Headers = {
    "rowid", "Identificador", "Aplicacao", "Informacao", 
    "Tipo", "Respondido", "Recebido", "Status", "Instancia"
  };
  private static readonly Int32[] ColumnWidths = { 13, 16, 10, 4, 20, 20, 6, 9 };
  private static Int32 GetColumnPosition(int columnIndex)
  {
    var position = 0;
    for (int i = 0; i < columnIndex; i++)
    {
      position += ColumnWidths[i];
    }
    return position;
  }
  private static void DrawData(List<List<String>> data)
  {
    for (int i = 0; i < data.Count && i < Console.WindowHeight - 2; i++) // Ensure rows fit in window
    {
      for (int j = 0; j < data[i].Count && j < Headers.Length; j++) // Ensure columns fit headers
      {
        var value = data[i][j].PadRight(ColumnWidths[j]);
        Console.SetCursorPosition(GetColumnPosition(j), i + 1);
        Console.Write(value.Length > ColumnWidths[j] 
            ? value.Substring(0, ColumnWidths[j] - 1) + "…" 
            : value);
      }
    }
  }
  static FrontEnd()
  {
    var queque = HandleQueQue.GetInstance();
    // Hide the cursor
    Console.CursorVisible = false;
    // Clear the console
    Console.Clear();
    // Main loop
    while (true)
    {
      try
      {
        // Reset cursor position
        Console.SetCursorPosition(0, 0);
        // Simulate data retrieval
        var data = queque.Get();
        // Draw the datatable
        DrawData(data);
        // Check for user input
        if (Console.KeyAvailable)
        {
          var key = Console.ReadKey(intercept: true);
          // Stops if key pressed is 'Q'
          if (key.Key == ConsoleKey.Q)
          {
            break;
          }
        }
        // Refresh every second
        Thread.Sleep(1000);
      }
      catch (System.Exception erro)
      {
        Console.Clear();
        Console.WriteLine($"Error: {erro.Message}");
        break;
      }
    }
  }
}