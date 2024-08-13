namespace telbot.API;
public class Response
{
  public Int32 status { get; set; }
  public String? message { get; set; }
  public List<String>? table { get; set; }
}