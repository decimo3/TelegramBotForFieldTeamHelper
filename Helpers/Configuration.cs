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
  public readonly string HOSTNAME;
  public Configuration(string[] args)
  {
    HOSTNAME = System.Environment.GetEnvironmentVariable("COMPUTERNAME")!;
    if(HOSTNAME is null) throw new InvalidOperationException("Environment variable HOSTNAME is not set!");
    CURRENT_PATH = System.IO.Directory.GetCurrentDirectory();
    SAP_SCRIPT = CURRENT_PATH + @"\sap.exe";
    IMG_SCRIPT = CURRENT_PATH + @"\img.exe";
    BOT_TOKEN = System.Environment.GetEnvironmentVariable("BOT_TOKEN")!;
    if(BOT_TOKEN is null) throw new InvalidOperationException("Environment variable BOT_TOKEN is not set!");
    if(!Validador.isValidToken(BOT_TOKEN)) throw new InvalidOperationException("Environment variable BOT_TOKEN is not valid!");
    ID_ADM_BOT = Int64.Parse(System.Environment.GetEnvironmentVariable("ID_ADM_BOT")!);
    if(ID_ADM_BOT == 0) throw new InvalidOperationException("Environment variable ID_ADM_BOT is not set!");
    DIAS_EXPIRACAO = 30;
    INSTANCIA = 0; // valor padrão caso não encontre o argumento no loop
    GERAR_FATURAS = true; // valor padrão caso não encontre o argumento no loop
    SAP_OFFLINE = false; // valor padrão caso não encontre o argumento no loop
    var env = System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
    if(env is null) IS_DEVELOPMENT = false;
    else IS_DEVELOPMENT = (env == "Development") ? true : false;
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
        if(Int32.TryParse(arg.Split("=")[1], out int instancia)) INSTANCIA = instancia;
        else throw new InvalidOperationException("Argumento 'instancia' não está no formato correto! Use the format: '--sap-instancia=<numInstancia>'");
      }
      if(arg.StartsWith("--bot-token"))
      {
        try
        {
          BOT_TOKEN = arg.Split("=")[1];
          if(!Validador.isValidToken(BOT_TOKEN)) throw new InvalidOperationException("Environment variable BOT_TOKEN is not valid!");
        }
        catch
        {
          throw new InvalidOperationException("Argumento 'token' não está no formato correto! Use the format: '--bot-token=<tokenDoBot>'");
        }
      }
    }
  }
}