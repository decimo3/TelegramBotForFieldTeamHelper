namespace telbot;
public class Users
{
  public string? name {get; set;}
  public long Id {get; set;}
  public DateTime exp {get; set;}
  public Role role { get; set;}
  public enum Role {Eletricista, Administrativo}
}