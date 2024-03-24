namespace telbot.Helpers;
public static class Updater
{
  public static void Update(Configuration cfg)
  {
    Console.WriteLine($"< {DateTime.Now} Manager: Verificando se há novas versões do sistema chatbot...");
    if(!System.IO.Directory.Exists(cfg.UPDATE_PATH))
      throw new DirectoryNotFoundException();
    if(!System.IO.Directory.Exists(cfg.TEMP_FOLDER))
      throw new DirectoryNotFoundException();
    var version = CurrentVersion(cfg);
    var updates = ListUpdates(cfg);
    var update = HasUpdate(updates, version);
    if(update != null)
    {
      try
      {
        ClearTemp(cfg);
        Download(cfg, update);
        Unzip(cfg);
        // DbMods(db); not implemented yet
        Replace(cfg);
        ClearTemp(cfg);
        Restart();
      }
      catch (Exception erro)
      {
        Temporary.ConsoleWriteError($"< {DateTime.Now} Manager: Erro ao tentar atualizar o sistema chatbot!");
        if(cfg.IS_DEVELOPMENT)
        {
          Temporary.ConsoleWriteError(erro.Message);
          Temporary.ConsoleWriteError(erro.StackTrace!);
        }
      }
    }
  }
  public static DateTime CurrentVersion(Configuration cfg)
  {
    var VersionFilepath = @"./version";
    if(!System.IO.File.Exists(VersionFilepath))
      throw new FileNotFoundException();
    var version = System.IO.File.ReadAllText(VersionFilepath);
    var version_date = DateTime.ParseExact(version, "yyyyMMdd", null);
    Console.WriteLine($"< {DateTime.Now} Manager: Versão atual do sistema chatbot: {version_date.ToString("yyyyMMdd")}");
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
    if(update_string != null)
      Console.WriteLine($"< {DateTime.Now} Manager: Nova versão {update_string} do sistema chatbot encontrada!");
    else
      Console.WriteLine($"< {DateTime.Now} Manager: A versão atual {current.ToString("yyyyMMdd")} já é a versão mais recente!");
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
  public static void DbMods(Configuration cfg)
  {
    // TODO - Implement method to execute update script on database
    var scripts = System.IO.Directory.GetFiles(cfg.TEMP_FOLDER).Where(file => Path.GetExtension(file) == "sql");
    // Database.ExecuteScript(script);
    throw new NotImplementedException();
  }
  public static void Replace(Configuration cfg)
  {
    Console.WriteLine($"< {DateTime.Now} Manager: Atualizando o sistema chatbot, por favor aguarde...");
    var files = System.IO.Directory.GetFiles(cfg.TEMP_FOLDER).Select(file => { return System.IO.Path.GetFileName(file); });
    foreach (var file in files)
    {
      if(file == "sap.conf") continue;
      if(file == "telbot.exe") continue;
      if(file == "database.db") continue;
      var new_file = System.IO.Path.Combine(cfg.TEMP_FOLDER, file);
      var old_file = System.IO.Path.Combine("./", file);
      System.IO.File.Copy(new_file, old_file, true);
    }
    var new_version = System.IO.Path.Combine(cfg.TEMP_FOLDER, "telbot.exe");
    var current_filepath = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
    var temporary_filepath = current_filepath += ".old";
    System.IO.File.Move(current_filepath, temporary_filepath);
    System.IO.File.Copy(new_version, current_filepath, true);
  }
  public static void Restart()
  {
    Console.WriteLine($"< {DateTime.Now} Manager: Sistema chatbot atualizado com sucesso! Reiniciando...");
    var current_filepath = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
    System.Diagnostics.Process.Start(current_filepath);
    System.Environment.Exit(0);
  }
}