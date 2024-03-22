using System.ComponentModel.DataAnnotations;
namespace telbot;
public class UsersModel : IValidatableObject
{
  [Required]
  public long id {get; set;}
  [Required]
  public DateTime create_at {get; set;} = DateTime.Now;
  [Required]
  public DateTime update_at {get; set;} =  DateTime.MinValue;
  [Required]
  public userLevel has_privilege {get; set;} = userLevel.desautorizar;
  [Required]
  public long inserted_by {get; set;} = 0;
  [Required]
  public long phone_number {get; set;} = 0;
  public UsersModel() {}
  public UsersModel(long id, long inserted_by)
  {
    this.id = id;
    this.inserted_by = inserted_by;
    this.update_at = DateTime.Now;
    this.has_privilege = userLevel.eletricista;
  }
  public UsersModel(long id)
  {
    this.id = id;
  }
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
    if(this.has_privilege == userLevel.supervisor) return true;
    if(this.has_privilege == userLevel.administrador) return true;
    if(this.has_privilege == userLevel.proprietario) return true;
    return false;
  }
  public bool pode_promover()
  {
    if(this.has_privilege == userLevel.administrador) return true;
    if(this.has_privilege == userLevel.proprietario) return true;
    return false;
  }
  public bool pode_transmitir()
  {
    if(this.has_privilege == userLevel.comunicador) return true;
    if(this.has_privilege == userLevel.administrador) return true;
    if(this.has_privilege == userLevel.proprietario) return true;
    return false;
  }
  public bool pode_consultar()
  {
    if(this.has_privilege == userLevel.comunicador) return false;
    if(this.has_privilege == userLevel.desautorizar) return false;
    return true;
  }
  public bool pode_relatorios()
  {
    if(this.has_privilege == userLevel.controlador) return true;
    if(this.has_privilege == userLevel.supervisor) return true;
    if(this.has_privilege == userLevel.administrador) return true;
    if(this.has_privilege == userLevel.proprietario) return true;
    return false;
  }
}
