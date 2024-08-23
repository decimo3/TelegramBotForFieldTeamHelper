namespace telbot.API;
public class Response
{
  public Int32 status { get; set; }
  public List<Entities> entities { get; set; } = new();
}
public class Entities
{
  public typeEntity type { get; set; }
  public String data { get; set; }
}
public enum typeEntity {TXT, PIC, XLS, PDF, XYZ}