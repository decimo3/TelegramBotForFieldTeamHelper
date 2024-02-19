namespace telbot.models;
public class Request
{
  public string? aplicacao { get; set; }
  public string? informacao { get; set; }
  public TypeRequest? tipo { get; set; }
  public DateTime received_at { get; set; }
}
public enum TypeRequest {gestao, comando, txtInfo, pdfInfo, picInfo, xlsInfo, anyInfo}