namespace telbot.handle;
using telbot.models;
public class HandleCommand
{
  private HandleMessage bot;
  private Request request;
  private UsersModel user;
  private Configuration cfg;
  public HandleCommand(HandleMessage bot, UsersModel user, Request request, Configuration cfg)
  {
    this.bot = bot;
    this.user = user;
    this.request = request;
    this.cfg = cfg;
  }
  async public Task routerCommand()
  {
    switch (request.aplicacao)
    {
      case "/start":
        await bot.sendTextMesssageWraper(user.id, "Seja bem vindo ao programa de automação de respostas do MestreRuan");
        await bot.sendTextMesssageWraper(user.id, "Se tiver em dúvida de como usar o bot, digite /ajuda.");
        break;
      case "/ajuda":
        await bot.sendTextMesssageWraper(user.id, "Digite o tipo de informação que deseja e depois o número da nota ou instalação.");
        await bot.sendTextMesssageWraper(user.id, "*TELEFONE* ou *CONTATO* para receber todos os telefones no cadastro do cliente;");
        await bot.sendTextMesssageWraper(user.id, "*COORDENADA* para receber um link da localização no cadastro do cliente;");
        await bot.sendTextMesssageWraper(user.id, "*ROTEIRO* para receber a lista de instalações ordenada por horário;");
        await bot.sendTextMesssageWraper(user.id, "*LEITURISTA* para receber a lista de instalações ordenada por sequencia;");
        await bot.sendTextMesssageWraper(user.id, "*PENDENTE* para receber a lista de débitos para aquela instalação do cliente;");
        await bot.sendTextMesssageWraper(user.id, "*FATURA* ou *DEBITO* _(sem acentuação)_ para receber as faturas vencidas em PDF (limite de 5 faturas)");
        await bot.sendTextMesssageWraper(user.id, "*HISTORICO* _(sem acentuação)_ para receber a lista com os 5 últimos serviços para a instalação;");
        await bot.sendTextMesssageWraper(user.id, "*MEDIDOR* para receber as informações referentes ao medidor;");
        await bot.sendTextMesssageWraper(user.id, "*AGRUPAMENTO* para receber as informações referentes ao PC;");
        await bot.sendTextMesssageWraper(user.id, "*INFORMACAO* para receber informações código e CPF do cliente;");
        await bot.sendTextMesssageWraper(user.id, "*CRUZAMENTO* para receber as ruas que cruzam com o logradouro da nota;");
        await bot.sendTextMesssageWraper(user.id, "*CONSUMO* para receber informações das últimas leituras e consumos do cliente;");
        await bot.sendTextMesssageWraper(user.id, "*ACESSO* para receber as COORDENADAS, LEITURISTA e CRUZAMENTOS de uma vez só;");
        await bot.sendTextMesssageWraper(user.id, "*ABERTURA* para receber o resultado da análise automática da instalação para abertura de nota de recuperação;");
        await bot.sendTextMesssageWraper(user.id, "Todas as solicitações não possuem acentuação e são no sigular (não tem o 's' no final).");
        break;
      case "/ping":
        await bot.sendTextMesssageWraper(user.id, "Estou de prontidão aguardando as solicitações! (^.^)");
        break;
      case "/status":
        if(user.id == (Int64)1469480868)
        {
          await using Stream stream = System.IO.File.OpenRead(@$"{cfg.CURRENT_PATH}\database.db");
          await bot.SendDocumentAsyncWraper(user.id, stream, $"{DateTime.Now.ToLocalTime().ToString("yyyyMMdd_HHmmss")}.db");
          stream.Dispose();
        }
        else
        {
          await bot.sendTextMesssageWraper(user.id, "Você não tem permissão para usar esse comando!");
        }
        break;
      default:
        await bot.sendTextMesssageWraper(user.id, "Comando solicitado não foi programado! Verifique e tente um válido");
        break;
    }
    return;
  }
}