using System.Text.RegularExpressions;
namespace telbot;
public static class Validador
{
  public static bool isValidAplicacao (string aplicacao)
  {
    var regex = new Regex("^[a-z]{6,16}$");
    return regex.IsMatch(aplicacao);
  }
  public static bool isValidInformacao (string informacao)
  {
    return Int64.TryParse(informacao, out long a);
  }
  public static bool isAplicacaoOption (string aplicacao)
  {
    if(aplicacao == "telefone") return true;
    if(aplicacao == "coordenada") return true;
    if(aplicacao == "localização") return true;
    if(aplicacao == "leiturista") return true;
    if(aplicacao == "roteiro") return true;
    if(aplicacao == "fatura") return true;
    if(aplicacao == "debito") return true;
    if(aplicacao == "historico") return true;
    if(aplicacao == "contato") return true;
    if(aplicacao == "autorizar") return true;
    if(aplicacao == "promover") return true;
    if(aplicacao == "atualizar") return true;
    if(aplicacao == "agrupamento") return true;
    if(aplicacao == "pendente") return true;
    if(aplicacao == "relatorio") return true;
    if(aplicacao == "manobra") return true;
    if(aplicacao == "medidor") return true;
    if(aplicacao == "passivo") return true;
    if(aplicacao == "suspenso") return true;
    return false;
  }
  public static bool orderOperandos (string info1, string info2)
  {
    if(isValidAplicacao(info1) && isValidInformacao(info2) && isAplicacaoOption(info1)) return true;
    if(isValidInformacao(info1) && isValidAplicacao(info2) && isAplicacaoOption(info2)) return false;
    throw new InvalidOperationException("Não foi estabelecida a ordem dos operandos ou não é uma aplicacao válida.");
  }
  public static bool isValidToken(string token)
  {
    var regex = new Regex("^[0-9]{8,10}:[a-zA-Z0-9_-]{35}$");
    return regex.IsMatch(token);
  }
}