namespace telbot;
public class Configuration
{
  public readonly string BOT_TOKEN;
  public readonly long ID_ADM_BOT;
  public readonly bool IS_DEVELOPMENT;
  public readonly string CURRENT_PATH;
  public readonly string SAP_SCRIPT;
  public readonly string IMG_SCRIPT;
  public readonly int DIAS_EXPIRACAO;
  public readonly bool GERAR_FATURAS;
  public readonly bool SAP_OFFLINE;
  public readonly int INSTANCIA;
  public Configuration(string[] args)
  {
    CURRENT_PATH = System.IO.Directory.GetCurrentDirectory();
    SAP_SCRIPT = CURRENT_PATH + @"\sap.exe";
    IMG_SCRIPT = CURRENT_PATH + @"\img.exe";
    BOT_TOKEN = System.Environment.GetEnvironmentVariable("BOT_TOKEN")!;
    if(BOT_TOKEN is null) throw new InvalidOperationException("Environment variable BOT_TOKEN is not set!");
    ID_ADM_BOT = Int64.Parse(System.Environment.GetEnvironmentVariable("ID_ADM_BOT")!);
    if(ID_ADM_BOT == 0) throw new InvalidOperationException("Environment variable ID_ADM_BOT is not set!");
    DIAS_EXPIRACAO = 30;
    GERAR_FATURAS = true; // valor padrão caso não encontre o argumento no loop
    SAP_OFFLINE = false; // valor padrão caso não encontre o argumento no loop
    IS_DEVELOPMENT = (System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") is null) ? true : false;
    foreach (var arg in args)
    {
      if(arg == "--sem-faturas") GERAR_FATURAS = false;
      if(arg == "--sap-offline") SAP_OFFLINE = true;
      if(arg == "--em-desenvolvimento")
      {
        INSTANCIA = 1;
        IS_DEVELOPMENT = true;
      }
      if(arg.StartsWith("--sap-instancia"))
      {
        var a = 0;
        if(Int32.TryParse(arg.Split("=")[1], out a)) INSTANCIA = a;
        else throw new InvalidOperationException("Argumento 'instancia' não está no formato correto! Use the format: '--sap-instancia=<numInstancia>'");
      }
    }
  }
}