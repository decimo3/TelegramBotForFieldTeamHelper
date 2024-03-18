using System.ComponentModel.DataAnnotations;
namespace telbot;
public class UsersModel : IValidatableObject
{
  [Required]
  public long id {get; set;}
  [Required]
  public DateTime create_at {get; set;} 
  [Required]
  public DateTime update_at {get; set;}
  [Required]
  public userLevel has_privilege {get; set;}
  [Required]
  public long inserted_by {get; set;}
  [Required]
  public long phone_number {get; set;}
  public UsersModel() {}
  public UsersModel(long id, long inserted_by)
  {
    this.id = id;
    this.inserted_by = inserted_by;
    this.create_at = DateTime.Now;
    this.update_at = DateTime.Now;
    this.has_privilege = userLevel.eletricista;
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
    monitorador = 2,
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
    if(this.has_privilege == userLevel.monitorador) return true;
    if(this.has_privilege == userLevel.supervisor) return true;
    if(this.has_privilege == userLevel.administrador) return true;
    if(this.has_privilege == userLevel.proprietario) return true;
    return false;
  }
}
