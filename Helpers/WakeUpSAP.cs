namespace telbot;
public class WakeUpSAP
{
  private readonly Configuration cfg;
  public WakeUpSAP(Configuration cfg)
  {
    this.cfg = cfg;
  }
  public void Start()
  {
    Console.WriteLine($"< {DateTime.Now} Manager: Iniciado sistema de despertador do SapAutomation!");
    while(true)
    {
      var horario = System.IO.File.GetLastWriteTime("database.db");
      var prazo = DateTime.Now.AddMinutes(-5);
      var diferenca = horario - prazo;
      Console.WriteLine($"< {DateTime.Now} Manager: Última solicitação registrada às {horario}.");
      if(diferenca.TotalMinutes < 0)
      {
        Console.WriteLine($"< {DateTime.Now} Manager: Realizando consulta para manter o SAP acordado...");
          var resposta = Temporary.executar(cfg, "desperta", "1380763967");
          if(resposta.Count == 0) resposta.Add("ERRO: O SAP demorou muito tempo na solicitação e foi encerrado!");
          if(resposta[0].StartsWith("ERRO"))
          {
            Console.Beep();
            Console.BackgroundColor = ConsoleColor.Red;
            Console.WriteLine($"< {DateTime.Now} Manager: A solicitação não pode ser concluída!");
            Console.ResetColor();
            Console.WriteLine($"< {DateTime.Now} Manager: Tentando novamente em daqui a 5 minutos...");
            Database.inserirRelatorio(new logsModel(0,"desperta", "0", false, DateTime.Now));
          }
          else
          {
            Console.WriteLine($"< {DateTime.Now} Manager: Consulta fantasma realizada com sucesso!");
            Database.inserirRelatorio(new logsModel(0,"desperta", "0", true, DateTime.Now));
          }
        }
      Console.WriteLine($"< {DateTime.Now} Manager: Última verificação foi realizada agora mesmo.");
      System.Threading.Thread.Sleep(cfg.ESPERA);
      }
    }
}