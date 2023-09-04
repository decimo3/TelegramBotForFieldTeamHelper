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
  public bool has_privilege {get; set;}
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
    this.has_privilege = false;
  }
  public IEnumerable<ValidationResult> Validate (ValidationContext context)
  {
    var results = new List<ValidationResult>();
    Validator.TryValidateProperty(this, context, results);
    return results;
  }
}