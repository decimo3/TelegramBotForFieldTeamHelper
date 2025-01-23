using System.Linq.Expressions;
using telbot.models;
namespace telbot.handle;
public class HandleQueQue
{
  private static readonly List<logsModel> lista = new();
  private static HandleQueQue? _instance = null;
  private HandleQueQue() {}
  public static HandleQueQue GetInstance()
  {
    lock(lista)
    {
      _instance ??= new();
      return _instance;
    }
  }
  public List<logsModel> Get()
  {
    lock(lista)
    {
      return new List<logsModel>(lista);
    }
  }
  public List<logsModel>? Get(Expression<Func<logsModel, bool>> expression)
  {
    if (expression is null)
      throw new ArgumentNullException();
    lock(lista)
    {
      return lista.Where(expression.Compile()).ToList();
    }
  }
  public void Add(logsModel request)
  {
    if (request == null)
      throw new ArgumentNullException();
    lock(lista)
    {
      lista.Add(request);
    }
  }
  public void Del(logsModel request)
  {
    if (request == null)
      throw new ArgumentNullException();
    lock(lista)
    {
      if(!lista.Remove(request))
        throw new ObjectNotFoundException();
    }
  }
  public void Alt(logsModel request)
  {
    if (request == null)
      throw new ArgumentNullException();
    lock(lista)
    {
      var item = lista.Find(l => l.rowid == request.rowid) ??
        throw new ObjectNotFoundException();
      lista.Remove(item);
      lista.Add(request);
    }
  }
  public class ObjectNotFoundException : Exception
  {
    public ObjectNotFoundException() : base() {}
    public ObjectNotFoundException(String message) : base(message) {}
  }
}