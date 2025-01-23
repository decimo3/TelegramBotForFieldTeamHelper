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
      throw new ArgumentNullException(nameof(expression));
    lock(lista)
    {
      return lista.Where(expression.Compile()).ToList();
    }
  }
  public void Add(logsModel request)
  {
    if (request == null)
      throw new ArgumentNullException(nameof(request));
    lock(lista)
    {
      lista.Add(request);
    }
  }
  public void Del(logsModel request)
  {
    if (request == null)
      throw new ArgumentNullException(nameof(request));
    lock(lista)
    {
      if(!lista.Remove(request))
        throw new ObjectNotFoundException(nameof(request));
    }
  }
  public void Alt(logsModel request)
  {
    if (request == null)
      throw new ArgumentNullException(nameof(request));
    lock(lista)
    {
      var item = lista.Find(l => l.rowid == request.rowid) ??
        throw new ObjectNotFoundException(nameof(request));
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