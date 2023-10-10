namespace telbot;
public class WakeUpSAP
{
  private readonly Int32 tempo = 5 * 60 * 1_000;
  public WakeUpSAP(Configuration cfg)
  {
    Console.WriteLine("> Manager: Iniciado sistema de despertador do SapAutomation!");
    while(true)
    {
      var horario = System.IO.File.GetLastWriteTime("database.db");
      var prazo = DateTime.Now.AddMinutes(-5);
      var diferenca = horario - prazo;
      Console.WriteLine($"> Manager: Última solicitação registrada às {horario}.");
      if(diferenca.TotalMinutes < 0)
      {
        Console.WriteLine("> Manager: Realizando consulta para manter o SAP acordado...");
          var resposta = Temporary.executar(cfg, "desperta", "1380763967");
          if(resposta[0].StartsWith("ERRO"))
          {
            Console.Beep();
            Console.BackgroundColor = ConsoleColor.Red;
            Console.WriteLine("> Manager: A solicitação não pode ser concluída!");
            Console.ResetColor();
            Console.WriteLine("> Manager: Tentando novamente em daqui a 1 minuto...");
            Database.inserirRelatorio(new logsModel(0,"desperta", "0", false, DateTime.Now));
            this.tempo = 1 * 60 * 1_000;
          }
          else
          {
            Console.WriteLine("> Manager: Consulta fantasma realizada com sucesso!");
            Database.inserirRelatorio(new logsModel(0,"desperta", "0", true, DateTime.Now));
            this.tempo = 5 * 60 * 1_000;
          }
        }
      Console.WriteLine($"> Manager: Última verificação realizada às {DateTime.Now}.");
      System.Threading.Thread.Sleep(tempo);
      }
    }
}