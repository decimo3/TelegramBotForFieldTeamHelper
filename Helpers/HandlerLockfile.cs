namespace telbot.Helpers;
public static class HandlerLockfile
{
  public static String VerificarLockfile(String lockfile)
  {
    lockfile = System.IO.Path.Combine(
      System.AppContext.BaseDirectory,
      lockfile
    );
    if(!System.IO.File.Exists(lockfile))
      return String.Empty;
    var texto = System.IO.File.ReadAllText(
      lockfile,
      System.Text.Encoding.UTF8
    );
    return (texto.Length < 50) ? String.Empty : texto;
  }
  public static void EscreverLockFile(String lockfile, String texto)
  {
    lockfile = System.IO.Path.Combine(
      System.AppContext.BaseDirectory,
      lockfile
    );
    System.IO.File.WriteAllText(lockfile, texto);
  }
  public static void EscreverLockFile(String lockfile, String application, Int64 information)
  {
    lockfile = System.IO.Path.Combine(
      System.AppContext.BaseDirectory,
      lockfile
    );
    var texto = $"{application} {information}";
    System.IO.File.WriteAllText(lockfile, texto);
  }
}