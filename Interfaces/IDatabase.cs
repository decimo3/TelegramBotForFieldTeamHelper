using System.Linq.Expressions;
using telbot.models;
namespace telbot.Interfaces;
public interface IDatabase : IDisposable
{
  public void InserirUsuario(UsersModel usuario);
  public UsersModel? RecuperarUsuario(Int64 identificador);
  public List<UsersModel> RecuperarUsuario(Expression<Func<UsersModel, bool>>? expression = null);
  public void AlterarUsuario(UsersModel usuario);

  public void InserirSolicitacao(logsModel request);
  public logsModel? RecuperarSolicitacao(Int64 rowid);
  public List<logsModel> RecuperarSolicitacao(Expression<Func<logsModel, bool>>? expression = null);
  public void AlterarSolicitacao(logsModel request);

  public void InserirFatura(pdfsModel pdf);
  public pdfsModel RecuperarFatura(Int64 rowid);
  public List<pdfsModel> RecuperarFatura(Expression<Func<pdfsModel, bool>>? expression = null);
  public void AlterarFatura(pdfsModel pdfInfo);
}
