using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace telbot.models;
public class logsModel
{
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public Int64 rowid { get; set; }
  public Int64 identifier { get; set; }
  public String application { get; set; } = String.Empty;
  public Int64 information { get; set; }
  public TypeRequest? typeRequest { get; set; }
  public DateTime received_at { get; set; }
  public DateTime response_at { get; set; }
  public Int32 instance { get; set; }
  public Int32 status { get; set; }
}
public enum TypeRequest {gestao, comando, txtInfo, pdfInfo, picInfo, xlsInfo, prlInfo, xyzInfo, ofsInfo, gpsInfo}