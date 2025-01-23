using System.Linq.Expressions;
using telbot.Interfaces;
using telbot.models;
using telbot.Services;
namespace telbot.handle;
public class HandleQueQue
{
  private static readonly List<logsModel> lista = new();
  private static HandleQueQue? _instance = null;
  private readonly IDatabase? database = null;
  private HandleQueQue(Configuration configuration)
  {
    this.database = Database.GetInstance(configuration);
    foreach (var request in database.RecuperarSolicitacao())
    {
      lista.Add(request);
    }
  }
  public static HandleQueQue GetInstance(Configuration? configuration = null)
  {
    lock(lista)
    {
      if (_instance is null)
      {
        if (configuration is null)
        {
          throw new NullReferenceException(
            "Database must be instantiated with a valid Configuration object.");
        }
        _instance = new(configuration);
      }
      return _instance;
    }
  }
  public List<logsModel> Get(Int32 limit)
  {
    lock(lista)
    {
      return new List<logsModel>(lista.Take(limit).ToList());
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