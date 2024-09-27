using Microsoft.Extensions.Logging;
using telbot.Services;
namespace telbot.Helpers;
public class Updater
{
  private static readonly String UPDATE_PATH = @"\\192.168.10.213\chatbot\";
  private static readonly String TEMPORARY_PATH = System.IO.Path.GetTempPath();
  public static void Update()
  {
    var logger = Logger.GetInstance<Updater>();
    try
    {
    if(!System.IO.Directory.Exists(UPDATE_PATH))
      throw new DirectoryNotFoundException();
    var version = CurrentVersion();
    logger.LogInformation("Versão atual do sistema chatbot: {version}", version.ToString("yyyyMMdd"));
    var updates = ListUpdates();
    logger.LogInformation("Verificando se há novas versões do sistema chatbot...");
    var update = HasUpdate(updates, version);
    if(update == null)
    {
      logger.LogInformation("Não foram encontradas atualizações para o sistema.");
      return;
    }
    logger.LogInformation("Nova versão {update} do sistema chatbot encontrada! Baixando...", update);
    Download(update);
    logger.LogInformation("Download concluído! Descompactando arquivo de atualização...");
    Unzip(update);
    logger.LogInformation("Fechando programas aninhados ao sistema do chatbot...");
    TerminateAll();
    logger.LogInformation("Aplicando atualização do sistema chatbot, por favor aguarde...");
    Replace(update);
    logger.LogInformation("Sistema chatbot atualizado com sucesso! Reiniciando...");
    ClearTemp(update);
    Restart();
    }
    catch (Exception erro)
    {
      logger.LogError(erro, "Ocorreu um erro ao tentar procurar/aplicar as atualizações:");
    }
  }
  public static DateTime CurrentVersion()
  {
    var VersionFilepath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "version");
    if(!System.IO.File.Exists(VersionFilepath))
      throw new FileNotFoundException();
    var version = System.IO.File.ReadAllText(VersionFilepath);
    var re = new System.Text.RegularExpressions.Regex(@"[0-9]{8}");
    var version_date = DateTime.ParseExact(re.Match(version).Value, "yyyyMMdd", null);
    return version_date;
  }
  public static List<DateTime> ListUpdates()
  {
    var updates = System.IO.Directory.GetFiles(UPDATE_PATH);
    var files_without_path = updates.Select(file => { return System.IO.Path.GetFileNameWithoutExtension(file); });
    var updates_date = files_without_path.Select(update => DateTime.ParseExact(update, "yyyyMMdd", null)).ToList();
    updates_date.Sort();
    return updates_date;
  }
  public static String? HasUpdate(List<DateTime> updates, DateTime current)
  {
    var update = updates.FirstOrDefault(update => update > current);
    if(update == default(DateTime)) return null;
    var update_string = update.ToString("yyyyMMdd");
    return update_string;
  }
  public static void ClearTemp(String update)
  {
    var zip_path =
      System.IO.Path.Combine(
        TEMPORARY_PATH,
        update + ".zip"
    );
    System.IO.File.Delete(zip_path);
    var dir_path =
      System.IO.Path.Combine(
        TEMPORARY_PATH,
        update
    );
    System.IO.Directory.Delete(dir_path, true);
  }
  public static void Download(String update)
  {
    var update_file = update + ".zip";
    var update_filepath = System.IO.Path.Combine(UPDATE_PATH, update_file);
    var update_destpath = System.IO.Path.Combine(TEMPORARY_PATH, update_file);
    System.IO.File.Copy(update_filepath, update_destpath);
  }
  public static void Unzip(String update)
  {
    var update_destpath = System.IO.Path.Combine(TEMPORARY_PATH, update);
    if(System.IO.Directory.Exists(update_destpath))
      System.IO.Directory.CreateDirectory(update_destpath);
    var update_filepath = System.IO.Path.Combine(TEMPORARY_PATH, update + ".zip");
    System.IO.Compression.ZipFile.ExtractToDirectory(update_filepath, update_destpath);
  }
  public static void Replace(String update)
  {
    var update_destpath = System.IO.Path.Combine(TEMPORARY_PATH, update);
    var files = System.IO.Directory.GetFiles(update_destpath)
      .Select(file => { return System.IO.Path.GetFileName(file); });
    foreach (var file in files)
    {
      if(file == "sap.conf") continue;
      if(file == "ofs.conf") continue;
      if(file == "bot.exe") continue;
      if(file == "database.db") continue;
      var new_file = System.IO.Path.Combine(update_destpath, file);
      var old_file = System.IO.Path.Combine(System.AppContext.BaseDirectory, file);
      System.IO.File.Copy(new_file, old_file, true);
    }
    var new_version = System.IO.Path.Combine(update_destpath, "bot.exe");
    var current_filepath = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
    var temporary_filepath = current_filepath + ".old";
    System.IO.File.Move(current_filepath, temporary_filepath);
    System.IO.File.Copy(new_version, current_filepath, true);
  }
  public static void Restart()
  {
    var arguments = System.Environment.GetCommandLineArgs();
    var executable = System.IO.Path.Combine(System.AppContext.BaseDirectory, "bot.exe");
    System.Diagnostics.Process.Start(executable, String.Join(' ', arguments.Skip(1).ToArray()));
    System.Environment.Exit(0);
  }
  public static void Terminate(String[] applications)
  {
    foreach (var process_name in applications)
    {
      if(String.IsNullOrEmpty(process_name)) continue;
      var argumentos = new String[] {"/F", "/T", "/IM", process_name};
      Executor.Executar("taskkill", argumentos, true);
    }
  }
  public static void TerminateAll()
  {
    Terminate(new String[] {"sap.exe", "saplpd.exe", "saplogon.exe"});
    Terminate(new String[] {"chrome.exe", "chromedriver.exe", "ofs.exe"});
    Terminate(new String[] {"chrome.exe", "chromedriver.exe", "prl.exe"});
  }
  public static Boolean IsChangedVersionFile()
  {
    var version_filepath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "version");
    var version_fileinfo = new System.IO.FileInfo(version_filepath);
    var version_filediff = DateTime.Now - version_fileinfo.LastWriteTime;
    return version_filediff.TotalMinutes < 5;
  }
  public static void UpdateVersionFile(DateTime datetime)
  {
    var version_filepath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "version");
    System.IO.File.WriteAllText(version_filepath, datetime.ToString("yyyyMMdd"));
  }
}