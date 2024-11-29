namespace telbot.models;
public class Fatura
{
  public String referencia { get; set; } = String.Empty;
  public String vencimento { get; set; } = String.Empty;
  public String montante { get; set; } = String.Empty;
  public override string ToString()
  {
    return 
      "Referencia: " + this.referencia +
      "Vencimento: " + this.vencimento +
      "Montante: R$ " + this.montante
    ;
  }
}