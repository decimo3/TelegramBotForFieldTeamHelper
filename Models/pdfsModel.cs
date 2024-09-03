using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace telbot.models;
public class pdfsModel
{
  public Int64 rowid { get; set; }
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.None)]
  public String filename { get; set; }
  public Int64 instalation { get; set; }
  public DateTime timestamp { get; set; }
  public Status status { get; set; }
  public Boolean has_expired()
  {
    return (DateTime.Now - this.timestamp).Minutes > 30;
  }
  public enum Status {wait, sent, done}
}