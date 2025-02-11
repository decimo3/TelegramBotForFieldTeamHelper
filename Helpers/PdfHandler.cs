using Microsoft.Extensions.Logging;
using telbot.Interfaces;
using telbot.Services;
using telbot.models;
using telbot.handle;
namespace telbot.Helpers;
public partial class PdfHandle
{
  private readonly IDatabase database;
  private readonly Configuration cfg;
  private readonly HandleMessage bot;
  private readonly ILogger logger;
  private readonly List<pdfsModel> faturas = new();
  private readonly Object _lock = new(); // Dedicated lock object
  public PdfHandle()
  {
    this.bot = HandleMessage.GetInstance();
    this.cfg = Configuration.GetInstance();
    this.database = Database.GetInstance();
    this.logger = Logger.GetInstance<PdfHandle>();
    // Delete all files in the temporary folder
    var files = Directory.GetFiles(cfg.TEMP_FOLDER);
    foreach (var file in files)
    {
      File.Delete(file);
    }
    var folders = Directory.GetDirectories(cfg.TEMP_FOLDER);
    
  }
}