namespace telbot.Helpers;
public static class Updater
{
  public static void Update(Configuration cfg)
  {
    try
    {
    ClearTemp(cfg);
    if(!System.IO.Directory.Exists(cfg.UPDATE_PATH))
      throw new DirectoryNotFoundException();
    if(!System.IO.Directory.Exists(cfg.TEMP_FOLDER))
      throw new DirectoryNotFoundException();
    var version = CurrentVersion(cfg);
    Console.WriteLine($"< {DateTime.Now} Manager: Versão atual do sistema chatbot: {version.ToString("yyyyMMdd")}");
    var updates = ListUpdates(cfg);
    Console.WriteLine($"< {DateTime.Now} Manager: Verificando se há novas versões do sistema chatbot...");
    var update = HasUpdate(updates, version);
    if(update == null)
    {
      Console.WriteLine($"< {DateTime.Now} Manager: Não foram encontradas atualizações para o sistema.");
      return;
    }
    Console.WriteLine($"< {DateTime.Now} Manager: Nova versão {update} do sistema chatbot encontrada! Baixando...");
    Download(cfg, update);
    Console.WriteLine($"< {DateTime.Now} Manager: Download concluído! Descompactando arquivo de atualização...");
    Unzip(cfg);
    Console.WriteLine($"< {DateTime.Now} Manager: Aplicando atualização do sistema chatbot, por favor aguarde...");
    Replace(cfg);
    Console.WriteLine($"< {DateTime.Now} Manager: Sistema chatbot atualizado com sucesso! Reiniciando...");
    Restart(cfg);
    }
    catch (Exception erro)
    {
      ConsoleWrapper.Error(Entidade.Updater, erro);
    }
  }
  public static DateTime CurrentVersion(Configuration cfg)
  {
    var VersionFilepath = @"./version";
    if(!System.IO.File.Exists(VersionFilepath))
      throw new FileNotFoundException();
    var version = System.IO.File.ReadAllText(VersionFilepath);
    var re = new System.Text.RegularExpressions.Regex(@"[0-9]{8}");
    var version_date = DateTime.ParseExact(re.Match(version).Value, "yyyyMMdd", null);
    return version_date;
  }
  public static List<DateTime> ListUpdates(Configuration cfg)
  {
    var updates = System.IO.Directory.GetFiles(cfg.UPDATE_PATH);
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
  public static void ClearTemp(Configuration cfg)
  {
    var files = System.IO.Directory.GetFiles(cfg.TEMP_FOLDER);
    foreach (var file in files)
    {
      System.IO.File.Delete(file);
    }
  }
  public static void Download(Configuration cfg, String update)
  {
    var update_file = update + ".zip";
    var update_filepath = System.IO.Path.Combine(cfg.UPDATE_PATH, update_file);
    var update_destpath = System.IO.Path.Combine(cfg.TEMP_FOLDER, "update.zip");
    System.IO.File.Copy(update_filepath, update_destpath);
  }
  public static void Unzip(Configuration cfg)
  {
    var update_filepath = System.IO.Path.Combine(cfg.TEMP_FOLDER, "update.zip");
    System.IO.Compression.ZipFile.ExtractToDirectory(update_filepath, cfg.TEMP_FOLDER);
    System.IO.File.Delete(update_filepath);
  }
  public static void Replace(Configuration cfg)
  {
    var files = System.IO.Directory.GetFiles(cfg.TEMP_FOLDER).Select(file => { return System.IO.Path.GetFileName(file); });
    foreach (var file in files)
    {
      if(file == "sap.conf") continue;
      if(file == "telbot.exe") continue;
      if(file == "database.db") continue;
      var new_file = System.IO.Path.Combine(cfg.TEMP_FOLDER, file);
      var old_file = System.IO.Path.Combine(cfg.CURRENT_PATH, file);
      System.IO.File.Copy(new_file, old_file, true);
    }
    var new_version = System.IO.Path.Combine(cfg.TEMP_FOLDER, "telbot.exe");
    var current_filepath = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
    var temporary_filepath = current_filepath + ".old";
    System.IO.File.Move(current_filepath, temporary_filepath);
    System.IO.File.Copy(new_version, current_filepath, true);
  }
  public static void Restart(Configuration cfg)
  {
    var executable = System.IO.Path.Combine(cfg.CURRENT_PATH, "telbot.exe");
    System.Diagnostics.Process.Start(executable);
    System.Environment.Exit(0);
  }
}