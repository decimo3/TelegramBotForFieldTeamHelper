using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace telbot.models;

public class DocsModel
{
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public Int64 rowid { get; set; }
  public String messageId { get; set; }
  public String filename { get; set; }
  public String identifier { get; set; }
  public DateTime updatedAt { get; set; }
  public Boolean IsOutdated { get; set; } = false;
}

