using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace telbot;
public class UsersModel : IValidatableObject
{
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public Int64 rowid { get; set; }
  [Required]
  public long identifier {get; set;}
  [Required]
  public DateTime create_at {get; set;} = DateTime.Now;
  [Required]
  public DateTime update_at {get; set;} =  DateTime.MinValue;
  [Required]
  public userLevel privilege {get; set;} = userLevel.desautorizar;
  [Required]
  public long inserted_by {get; set;} = 0;
  [Required]
  public long phone_number {get; set;} = 0;
  public String username { get; set; } = String.Empty;
  public IEnumerable<ValidationResult> Validate (ValidationContext context)
  {
    var results = new List<ValidationResult>();
    Validator.TryValidateProperty(this, context, results);
    return results;
  }
  public enum userLevel {
    desautorizar = -1,
    eletricista = 0,
    supervisor = 1,
    controlador = 2,
    comunicador = 3,
    administrador = 4,
    proprietario = 5,
  }
  public bool pode_autorizar()
  {
    if(this.privilege == userLevel.supervisor) return true;
    if(this.privilege == userLevel.administrador) return true;
    if(this.privilege == userLevel.proprietario) return true;
    return false;
  }
  public bool pode_promover()
  {
    if(this.privilege == userLevel.administrador) return true;
    if(this.privilege == userLevel.proprietario) return true;
    return false;
  }
  public bool pode_transmitir()
  {
    if(this.privilege == userLevel.comunicador) return true;
    if(this.privilege == userLevel.administrador) return true;
    if(this.privilege == userLevel.proprietario) return true;
    return false;
  }
  public bool pode_consultar()
  {
    if(this.privilege == userLevel.comunicador) return false;
    if(this.privilege == userLevel.desautorizar) return false;
    return true;
  }
  public bool pode_relatorios()
  {
    if(this.privilege == userLevel.controlador) return true;
    if(this.privilege == userLevel.supervisor) return true;
    if(this.privilege == userLevel.administrador) return true;
    if(this.privilege == userLevel.proprietario) return true;
    return false;
  }
  public Int32 dias_vencimento()
  {
    if(this.privilege == userLevel.eletricista)
      return (this.update_at.AddDays(30) - DateTime.Now).Days;
    if(this.privilege == userLevel.controlador)
      return (this.update_at.AddDays(30) - DateTime.Now).Days;
    if(this.privilege == userLevel.supervisor)
      return (this.update_at.AddDays(90) - DateTime.Now).Days;
    if(this.privilege == userLevel.comunicador)
      return 99;
    if(this.privilege == userLevel.administrador)
      return 99;
    if(this.privilege == userLevel.proprietario)
      return 99;
    return -1;
  }
}