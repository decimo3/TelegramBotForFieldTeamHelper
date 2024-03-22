namespace telbot;
public class Configuration
{
  public readonly string BOT_TOKEN = String.Empty;
  public readonly long ID_ADM_BOT = 0;
  public readonly bool IS_DEVELOPMENT = false;
  public readonly string CURRENT_PATH = String.Empty;
  public readonly string SAP_SCRIPT = String.Empty;
  public readonly string IMG_SCRIPT = String.Empty;
  public readonly int DIAS_EXPIRACAO = 30;
  public readonly bool GERAR_FATURAS = true;
  public readonly bool SAP_OFFLINE = false;
  public readonly int INSTANCIA = 0;
  public readonly string? CURRENT_PC;
  public readonly string? LICENCE;
  public readonly bool SAP_RESTRITO = false;
  public readonly int ESPERA = 60_000;
  public readonly string LOCKFILE = "sap.lock";
  public readonly int VENCIMENTOS = 0;
  public readonly string UPDATE_PATH = @"\\localhost\Shared\chatbot";
  public readonly string TEMP_FOLDER = String.Empty;
  public Configuration(string[] args)
  {
    LICENCE = System.Environment.GetEnvironmentVariable("BOT_LICENCE") ??
      throw new InvalidOperationException("Environment variable BOT_LICENCE is not set!");
    var AUTHORIZATION = Authorization.RecoveryToken(LICENCE) ??
      throw new InvalidOperationException("Token in environment variable BOT_LICENCE is not valid!");
    
    ID_ADM_BOT = AUTHORIZATION.adm_id_bot;
    
    var agora = DateTime.Now;
    var prazo = DateTimeOffset.FromUnixTimeSeconds(AUTHORIZATION.exp).DateTime;
    if(agora > prazo) throw new InvalidOperationException("O período de licença de uso expirou!");

    CURRENT_PC = System.Environment.GetEnvironmentVariable("COMPUTERNAME") ??
      throw new InvalidOperationException("Environment variable CURRENT_PC is not set!");
    if(CURRENT_PC != AUTHORIZATION.allowed_pc) throw new InvalidOperationException("A licença de uso não permite o uso em outra máquina!");
    
    CURRENT_PATH = System.IO.Directory.GetCurrentDirectory();
    TEMP_FOLDER = CURRENT_PATH + @"\tmp\";
    SAP_SCRIPT = CURRENT_PATH + @"\sap.exe";
    IMG_SCRIPT = CURRENT_PATH + @"\img.exe";
    
    BOT_TOKEN = System.Environment.GetEnvironmentVariable("BOT_TOKEN") ??
      throw new InvalidOperationException("Environment variable BOT_TOKEN is not set!");
    if(!Validador.isValidToken(BOT_TOKEN)) throw new InvalidOperationException("Environment variable BOT_TOKEN is not valid!");
    

    var env = System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
    if(env is null) IS_DEVELOPMENT = false;
    else IS_DEVELOPMENT = (env == "Development") ? true : false;

    foreach (var arg in args)
    {
      if(arg.StartsWith("--sap-instancia"))
      {
        if(Int32.TryParse(arg.Split("=")[1], out int instancia)) INSTANCIA = instancia;
        else throw new InvalidOperationException("Argumento 'instancia' não está no formato correto! Use the format: '--sap-instancia=<numInstancia>'");
        continue;
      }
      if(arg.StartsWith("--sap-espera"))
      { 
        if(Int32.TryParse(arg.Split("=")[1], out int espera)) ESPERA = espera * 1000;
        else throw new InvalidOperationException("Argumento 'espera' não está no formato correto! Use the format: '--sap-espera=<segundos_espera>'");
        continue;
      }
      if(arg.StartsWith("--vencimentos"))
      { 
        if(Int32.TryParse(arg.Split("=")[1], out int vencimento)) VENCIMENTOS = vencimento * 1000 * 60;
        else throw new InvalidOperationException("Argumento 'vencimentos' não está no formato correto! Use the format: '--vencimentos=<segundos_espera>'");
        continue;
      }
      switch (arg)
      {
        case "--sem-faturas": GERAR_FATURAS = false; break;
        case "--sap-offline": SAP_OFFLINE = true; break;
        case "--em-desenvolvimento": IS_DEVELOPMENT = true; break;
        case "--sap-restrito": SAP_RESTRITO = true; break;
        default: throw new InvalidOperationException($"O argumento {arg} é inválido!");
      }
    }
    if(SAP_OFFLINE && VENCIMENTOS > 0)
      throw new InvalidOperationException("Não é possível usar os argumentos '--vencimentos' e '--sap-offline' ao mesmo tempo");
  }
}