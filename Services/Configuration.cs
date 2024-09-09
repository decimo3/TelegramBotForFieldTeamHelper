using dotenv.net;
namespace telbot.Services;
public class Configuration
{
  public readonly string BOT_TOKEN = String.Empty;
  public readonly long ID_ADM_BOT = 0;
  public readonly bool IS_DEVELOPMENT = false;
  public readonly string CURRENT_PATH = String.Empty;
  public readonly bool GERAR_FATURAS = true;
  public readonly bool SAP_OFFLINE = false;
  public readonly int SAP_INSTANCIA = 1;
  public readonly string? CURRENT_PC;
  public readonly string? LICENCE;
  public readonly int SAP_ESPERA = 120_000;
  public readonly bool PRL_SUBSISTEMA = false;
  public readonly bool SAP_VENCIMENTO = false;
  public readonly bool SAP_BANDEIRADA = false;
  public readonly bool OFS_MONITORAMENTO = false;
  public readonly string SERVER_NAME = "192.168.10.213";
  public readonly string UPDATE_PATH = String.Empty;
  public readonly string TEMP_FOLDER = String.Empty;
  public readonly Int32 TASK_DELAY = 1_000;
  public readonly Int32 TASK_DELAY_LONG = 10_000;
  public readonly List<String> REGIONAIS = new();
  public readonly Dictionary<String, String> CONFIGURACAO = new();
  private static Configuration _instance;
  private static readonly Object _lock = new object();
  public static Configuration GetInstance(string[]? args = null)
  {
    lock (_lock)
    {
      if (_instance == null)
      {
        if (args == null)
        {
          throw new InvalidOperationException("Configuration must be instantiated with a valid string array.");
        }
        _instance = new Configuration(args);
      }
      return _instance;
    }
  }
  private Configuration(string[] args)
  {
    foreach (var arg in args)
    {
      if(System.IO.File.Exists(arg)) continue;
      if(arg.StartsWith("--sap-instancia"))
      {
        if(Int32.TryParse(arg.Split("=")[1], out int instancia)) SAP_INSTANCIA = instancia;
        else throw new InvalidOperationException("Argumento 'instancia' não está no formato correto! Use the format: '--sap-instancia=<numInstancia>'");
        continue;
      }
      if(arg.StartsWith("--sap-espera"))
      { 
        if(Int32.TryParse(arg.Split("=")[1], out int espera)) SAP_ESPERA = espera * 1000;
        else throw new InvalidOperationException("Argumento 'espera' não está no formato correto! Use the format: '--sap-espera=<segundos_espera>'");
        continue;
      }
      if(arg.StartsWith("--sap-crossover"))
      {
        var regionais_arg = arg.Split('=')[1];
        foreach (var regional in regionais_arg.Split(',').ToList())
        {
          this.REGIONAIS.Add(regional);
        }
        continue;
      }
      switch (arg)
      {
        case "--sem-faturas": GERAR_FATURAS = false; break;
        case "--sap-offline": SAP_OFFLINE = true; break;
        case "--em-desenvolvimento": IS_DEVELOPMENT = true; break;
        case "--sap-vencimento": SAP_VENCIMENTO = true; break;
        case "--sap-bandeirada": SAP_BANDEIRADA = true; break;
        case "--ofs-monitorador": OFS_MONITORAMENTO = true; break;
        case "--prl-subsistema": PRL_SUBSISTEMA = true; break;
        default: Ajuda(arg); break;
      }
    }
    if(SAP_OFFLINE && (SAP_VENCIMENTO || SAP_BANDEIRADA))
      throw new InvalidOperationException("Não é possível usar os argumentos '--sap-offline' e '--sap-vencimento' ou --sap-bandeirada ao mesmo tempo");

    if(IS_DEVELOPMENT == true) DotEnv.Load();

    BOT_TOKEN = System.Environment.GetEnvironmentVariable("BOT_TOKEN") ??
      throw new InvalidOperationException("Environment variable BOT_TOKEN is not set!");
    if(!Validador.isValidToken(BOT_TOKEN)) throw new InvalidOperationException("Environment variable BOT_TOKEN is not valid!");

    LICENCE = System.Environment.GetEnvironmentVariable("BOT_LICENCE") ??
      throw new InvalidOperationException("Environment variable BOT_LICENCE is not set!");
    var AUTHORIZATION = Authorization.RecoveryToken(LICENCE) ??
      throw new InvalidOperationException("Token in environment variable BOT_LICENCE is not valid!");

    CURRENT_PC = System.Environment.GetEnvironmentVariable("COMPUTERNAME") ??
      throw new InvalidOperationException("Environment variable CURRENT_PC is not set!");
    if(CURRENT_PC != AUTHORIZATION.allowed_pc) throw new InvalidOperationException("A licença de uso não permite o uso em outra máquina!");

    var prazo = DateTimeOffset.FromUnixTimeSeconds(AUTHORIZATION.exp).DateTime;
    if(DateTime.Now > prazo) throw new InvalidOperationException("O período de licença de uso expirou!");

    ID_ADM_BOT = AUTHORIZATION.adm_id_bot;
    CURRENT_PATH = System.IO.Directory.GetCurrentDirectory();
    TEMP_FOLDER = CURRENT_PATH + @"\tmp\";
    if(!System.IO.Directory.Exists(TEMP_FOLDER))
      System.IO.Directory.CreateDirectory(TEMP_FOLDER);
    UPDATE_PATH = @$"\\{SERVER_NAME}\chatbot\";

  }
  public Dictionary<String,String> ArquivoConfiguracao(String filename, char delimiter = '=')
  {
    var parametros = new Dictionary<string,string>();
    var file = System.IO.File.ReadAllLines(filename);
    foreach (var line in file)
    {
      if(String.IsNullOrEmpty(line)) continue;
      var args = line.Split(delimiter);
      if(args.Length != 2) continue;
      parametros.Add(args[0], args[1]);
    }
    return parametros;
  }
  public void Ajuda(String arg)
  {
    var ajuda = @$"
    O argumento {arg} é inválido!

    Instruções de uso: telbot.exe [options]
    --em-desenvolvimento   Usado para testes na aplicação;
    --sap-offline          Sinaliza ao usuários se o sistema SAP estiver fora do ar;
    --sem-faturas          Sinalizar ao usuários se o sistema SAP não estiver enviando faturas;
    --sap-instancia=0      Altera a instancia SAP a ser usada pelo chatbot;
    --sap-vencimento       Iniciar o subsistema de monitoramento de notas de vencimentos pelo SAP;
    --sap-bandeirada       Iniciar o subsistema de monitoramento de notas pendentes de bandeiradas pelo SAP;
    --sap-crossover=oeste  Permite o sistema SAP trabalhar com mais de uma regional;
    --ofs-monitorador      Iniciar o subsistema de monitoramento dos ofensores do IDG pelo OFS;
    --prl-subsistema       Permite o gerenciamento automático das janelas do subsistema do PRL;
    ";
    telbot.Helpers.ConsoleWrapper.Write(telbot.Helpers.Entidade.Chatbot, ajuda);
    System.Environment.Exit(1);
  }
}