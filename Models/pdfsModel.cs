using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace telbot.models;
public class pdfsModel
{
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public Int64 rowid { get; set; }
  public String filename { get; set; }
  public Int64 instalation { get; set; }
  public DateTime timestamp { get; set; }
  public Boolean has_expired()
  {
    return (DateTime.Now - this.timestamp).Days > 1;
  }
}