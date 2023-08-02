namespace telbot.models;
public class Request
{
  public string? aplicacao { get; set; }
  public string? informacao { get; set; }
  public TypeRequest? tipo { get; set; }
}
public enum TypeRequest {gestao, comando, txtInfo, pdfInfo, picInfo, xlsInfo}