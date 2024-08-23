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
    var regex = new Regex("^[a-z0-9]{4,16}$");
    return regex.IsMatch(aplicacao);
  }
  public static bool isValidInformacao (string informacao)
  {
    return Int64.TryParse(informacao, out long a);
  }
  public static TypeRequest? isAplicacaoOption (string aplicacao)
  {
    if(aplicacao == "telefone") return TypeRequest.txtInfo;
    if(aplicacao == "coordenada") return TypeRequest.xyzInfo;
    if(aplicacao == "localizacao") return TypeRequest.xyzInfo;
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
    if(aplicacao == "bandeirada") return TypeRequest.xlsInfo;
    if(aplicacao == "medidor") return TypeRequest.txtInfo;
    if(aplicacao == "informacao") return TypeRequest.txtInfo;
    if(aplicacao == "cruzamento") return TypeRequest.picInfo;
    if(aplicacao == "consumo") return TypeRequest.picInfo;
    if(aplicacao == "abertura") return TypeRequest.txtInfo;
    if(aplicacao == "ren360") return TypeRequest.picInfo;
    if(aplicacao == "desautorizar") return TypeRequest.gestao;
    if(aplicacao == "controlador") return TypeRequest.gestao;
    if(aplicacao == "comunicador") return TypeRequest.gestao;
    if(aplicacao == "administrador") return TypeRequest.gestao;
    if(aplicacao == "supervisor") return TypeRequest.gestao;
    if(aplicacao == "leitura") return TypeRequest.picInfo;
    if(aplicacao == "vencimento") return TypeRequest.xlsInfo;
    if(aplicacao == "evidencia") return TypeRequest.ofsInfo;
    if(aplicacao == "codbarra") return TypeRequest.txtInfo;
    if(aplicacao == "fuga") return TypeRequest.picInfo;
    if(aplicacao == "zona") return TypeRequest.picInfo;
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
  public static logsModel? isRequest(string text)
  {
    text = text.ToLower();
    var request = new logsModel();
    if(!isValidArguments(text)) return null;
    var args = text.Split(" ");
    if (args[0].StartsWith("/"))
    {
      request.application = args[0];
      request.information = 0;
      request.typeRequest = TypeRequest.comando;
      return request;
    }
    else
    {
      if(args.Length == 1) return null;
      var estaNaNaOrdemCerta = Validador.orderOperandos(args[0], args[1]);
      if(estaNaNaOrdemCerta is null) return null;
      request.application = ((bool)estaNaNaOrdemCerta) ? args[0] : args[1];
      request.information = ((bool)estaNaNaOrdemCerta) ? Int64.Parse(args[1]) : Int64.Parse(args[0]);
      request.typeRequest = isAplicacaoOption(request.application);
      if(request.typeRequest is null) return null;
      return request;
    }
  }
}
