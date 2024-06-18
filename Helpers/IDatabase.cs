using System.Linq.Expressions;
namespace telbot.Interfaces
{
  public interface IDatabase
  {
    // users
    public void inserirUsuario(UsersModel user_model, Int64 inserted_by);
    public void alterarUsuario(UsersModel user_model, Int64 inserted_by);
    public UsersModel recuperarUsuario(long id);
    public List<UsersModel> recuperarUsuario();
    public List<UsersModel> recuperarUsuario(Expression<Func<UsersModel, bool>> expression);
    // logs
    public void inserirRelatorio(logsModel logs);
    public logsModel recuperarRelatorio(String aplicacao, Int64 informacao);
    public List<logsModel> recuperarRelatorio();
    public List<logsModel> recuperarRelatorio(Expression<Func<logsModel, bool>> expression);
    //lost
    public void InserirPerdido(errorReport report);
    public List<errorReport> recuperarPerdido();
    public void excluirPerdidos();
  }
}
