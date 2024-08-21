namespace telbot.models;
public class RequestText
{
  public Int64 Identificador { get; set; }
  public String Mensagem { get; set; }
  public DateTime RecebidoEm { get; set; }
  public RequestText(Int64 identificador, String mensagem, DateTime received_at)
  {
    this.Identificador = identificador;
    this.Mensagem = mensagem;
    this.RecebidoEm = received_at;
  }
}
