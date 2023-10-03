namespace telbot;
public class Configuration
{
  public readonly string? BOT_TOKEN;
  public readonly long ID_ADM_BOT;
  public readonly bool IS_DEVELOPMENT;
  public readonly string? CURRENT_PATH;
  public readonly string? SAP_SCRIPT;
  public readonly string? IMG_SCRIPT;
  public readonly int DIAS_EXPIRACAO;
  public readonly bool GERAR_FATURAS;
  public readonly bool SAP_OFFLINE;
  public readonly int INSTANCIA;
  public readonly string? CURRENT_PC;
  public readonly string? LICENCE;
  public readonly bool SAP_RESTRITO;
  public Configuration(string[] args)
  {
    LICENCE = System.Environment.GetEnvironmentVariable("BOT_LICENCE");
    if(LICENCE is null) throw new InvalidOperationException("Environment variable BOT_LICENCE is not set!");
    var AUTHORIZATION = Authorization.RecoveryToken(LICENCE);
    if(AUTHORIZATION is null) throw new InvalidOperationException("Token in environment variable BOT_LICENCE is not valid!");
    
    var agora = DateTime.Now;
    var prazo = DateTimeOffset.FromUnixTimeSeconds(AUTHORIZATION.exp).DateTime;
    if(agora > prazo)
    {
      Console.BackgroundColor = ConsoleColor.Red;
      Console.Beep();
      Console.Write("O período de licença de uso expirou!");
      Console.BackgroundColor = ConsoleColor.Black;
      Console.WriteLine("Necessário entrar em contato com o administrador do sistema!");
      return;
    }
    
    CURRENT_PC = System.Environment.GetEnvironmentVariable("COMPUTERNAME");
    if(CURRENT_PC is null) throw new InvalidOperationException("Environment variable CURRENT_PC is not set!");
    if(CURRENT_PC != AUTHORIZATION.allowed_pc)
    {
      Console.BackgroundColor = ConsoleColor.Red;
      Console.Beep();
      Console.Write("A licença de uso não permite o uso em outra máquina!");
      Console.BackgroundColor = ConsoleColor.Black;
      Console.WriteLine("Necessário entrar em contato com o administrador do sistema!");
      return;
    }
    
    CURRENT_PATH = System.IO.Directory.GetCurrentDirectory();
    SAP_SCRIPT = CURRENT_PATH + @"\sap.exe";
    IMG_SCRIPT = CURRENT_PATH + @"\img.exe";
    
    BOT_TOKEN = System.Environment.GetEnvironmentVariable("BOT_TOKEN");
    if(BOT_TOKEN is null) throw new InvalidOperationException("Environment variable BOT_TOKEN is not set!");
    if(!Validador.isValidToken(BOT_TOKEN)) throw new InvalidOperationException("Environment variable BOT_TOKEN is not valid!");
    
    ID_ADM_BOT = AUTHORIZATION.adm_id_bot;
    DIAS_EXPIRACAO = 30;
    INSTANCIA = 0; // valor padrão caso não encontre o argumento no loop
    GERAR_FATURAS = true; // valor padrão caso não encontre o argumento no loop
    SAP_OFFLINE = false; // valor padrão caso não encontre o argumento no loop
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
      switch (arg)
      {
        case "--sem-faturas": GERAR_FATURAS = false; break;
        case "--sap-offline": SAP_OFFLINE = true; break;
        case "--em-desenvolvimento": IS_DEVELOPMENT = true; break;
        default: throw new InvalidOperationException($"O argumento {arg} é inválido!");
      }
    }
  }
}