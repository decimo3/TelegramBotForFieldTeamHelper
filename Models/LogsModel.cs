namespace telbot;
public class logsModel
{
  public long id {get; set;}
  public string? solicitacao {get; set;}
  public string? informacao {get; set;}
  public DateTime create_at {get; set;}
  public DateTime received_at { get; set; }
  public bool is_sucess {get; set;}
  public logsModel() {}
  public logsModel(long id, string? solicitacao, string? informacao, bool is_sucess, DateTime received_at)
  {
    this.id = id;
    this.solicitacao = solicitacao;
    this.informacao = informacao;
    this.create_at = DateTime.Now;
    this.is_sucess = is_sucess;
    this.received_at = received_at;
  }
}