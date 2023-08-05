namespace telbot;
using telbot.models;
using System.Text.RegularExpressions;
public static class Validador
{
  public static bool isValidArguments(string text)
  {
    if(!(text.Length > 0)) return false;
    return true;
  }
  public static bool isValidAplicacao (string aplicacao)
  {
    var regex = new Regex("^[a-z]{6,16}$");
    return regex.IsMatch(aplicacao);
  }
  public static bool isValidInformacao (string informacao)
  {
    return Int64.TryParse(informacao, out long a);
  }
  public static TypeRequest? isAplicacaoOption (string aplicacao)
  {
    if(aplicacao == "telefone") return TypeRequest.txtInfo;
    if(aplicacao == "coordenada") return TypeRequest.txtInfo;
    if(aplicacao == "localizacao") return TypeRequest.txtInfo;
    if(aplicacao == "leiturista") return TypeRequest.picInfo;
    if(aplicacao == "roteiro") return TypeRequest.picInfo;
    if(aplicacao == "fatura") return TypeRequest.pdfInfo;
    if(aplicacao == "debito") return TypeRequest.pdfInfo;
    if(aplicacao == "historico") return TypeRequest.picInfo;
    if(aplicacao == "contato") return TypeRequest.txtInfo;
    if(aplicacao == "autorizar") return TypeRequest.gestao;
    if(aplicacao == "promover") return TypeRequest.gestao;
    if(aplicacao == "atualizar") return TypeRequest.gestao;
    if(aplicacao == "agrupamento") return TypeRequest.picInfo;
    if(aplicacao == "pendente") return TypeRequest.picInfo;
    if(aplicacao == "relatorio") return TypeRequest.xlsInfo;
    if(aplicacao == "manobra") return TypeRequest.xlsInfo;
    if(aplicacao == "medidor") return TypeRequest.txtInfo;
    if(aplicacao == "passivo") return TypeRequest.pdfInfo;
    if(aplicacao == "suspenso") return TypeRequest.txtInfo;
    return null;
  }
  public static bool? orderOperandos (string info1, string info2)
  {
    if(isValidAplicacao(info1) && isValidInformacao(info2)) return true;
    if(isValidInformacao(info1) && isValidAplicacao(info2)) return false;
    return null;
  }
  public static bool isValidToken(string token)
  {
    var regex = new Regex("^[0-9]{8,10}:[a-zA-Z0-9_-]{35}$");
    return regex.IsMatch(token);
  }
  public static Request? isRequest(string text)
  {
    var request = new Request();
    if(!isValidArguments(text)) return null;
    var args = text.Split(" ");
    if (args[0].StartsWith("/"))
    {
      request.aplicacao = args[0];
      request.informacao = null;
      request.tipo = TypeRequest.comando;
      return request;
    }
    else
    {
      if(args.Length == 1) return null;
      var estaNaNaOrdemCerta = Validador.orderOperandos(args[0], args[1]);
      if(estaNaNaOrdemCerta is null) return null;
      request.aplicacao = ((bool)estaNaNaOrdemCerta) ? args[0] : args[1];
      request.informacao = ((bool)estaNaNaOrdemCerta) ? args[1] : args[0];
      request.tipo = isAplicacaoOption(request.aplicacao);
      if(request.tipo is null) return null;
      return request;
    }
  }
}