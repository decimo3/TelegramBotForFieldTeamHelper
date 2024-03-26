namespace telbot.handle;
using telbot.Helpers;
using telbot.models;
public static class Command
{
  public async static Task HandleCommand(HandleMessage bot, UsersModel user, Request request, Configuration cfg)
  {
    switch (request.aplicacao)
    {
      case "/start":
        await bot.sendTextMesssageWraper(user.id, "Seja bem vindo ao programa de automação de respostas do MestreRuan");
        await bot.sendTextMesssageWraper(user.id, "Se tiver em dúvida de como usar o bot, digite /ajuda.");
        break;
      case "/ajuda":
        await bot.sendTextMesssageWraper(user.id, "Para consultas de informações, digite a aplicação e depois o número da nota ou instalação.");
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
        await bot.sendTextMesssageWraper(user.id, "*REN360* para receber a lista dos consumos dos clientes próximos e passividade para abertura de nota de recuperação;");
        await bot.sendTextMesssageWraper(user.id, "Todas as solicitações não possuem acentuação e são no sigular (não tem o 's' no final).");
        if(!user.pode_autorizar()) break;
        await bot.sendTextMesssageWraper(user.id, "Para os comandos de gestão, digite o cargo e depois insira o número do identificador");
        await bot.sendTextMesssageWraper(user.id, "*AUTORIZAR* para cadastrar novos usuários com acesso de consulta no sistema chatbot");
        await bot.sendTextMesssageWraper(user.id, "*ATUALIZAR* para renovar o prazo de expiração um usuário com acesso de consulta do sistema");
        await bot.sendTextMesssageWraper(user.id, "*SUPERVISOR* para alterar para um usuário que pode autorizar outros usuários");
        await bot.sendTextMesssageWraper(user.id, "*DESAUTORIZAR* para remover o acesso de consulta de um usuário no sistema chatbot");
        await bot.sendTextMesssageWraper(user.id, "*MONITORADOR* para alterar para um usuário que pode receber avisos sobre outros usuários (aplicação futura)");
        await bot.sendTextMesssageWraper(user.id, "*COMUNICADOR* para alterar para um usuário capaz de enviar transmissões pelo sistema (aplicação futura)");
        break;
      case "/ping":
        await bot.sendTextMesssageWraper(user.id, "Estou de prontidão aguardando as solicitações! (^.^)");
        break;
      case "/status":
        if(user.has_privilege == UsersModel.userLevel.proprietario)
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
      case "/info":
        var info = new System.Text.StringBuilder();
        info.Append($"*Identificador:* {user.id}\n");
        info.Append($"*Telefone:* {user.phone_number}\n");
        info.Append($"*Autorização:* {user.has_privilege.ToString()}\n");
        if(user.has_privilege == UsersModel.userLevel.eletricista)
        {
          var prazo = user.update_at.AddDays(cfg.DIAS_EXPIRACAO);
          var dias = prazo - DateTime.Today;
          info.Append($"*Expiração:* {prazo.ToString("dd/MM/yyyy")} ({(int)dias.TotalDays} dias)\n");
        }
        info.Append($"*Versão:* {Updater.CurrentVersion(cfg).ToString("yyyyMMdd")}");
        await bot.sendTextMesssageWraper(user.id, info.ToString());
        break;
      case "/update":
        if(user.has_privilege != UsersModel.userLevel.administrador)
        {
          await bot.sendTextMesssageWraper(user.id, "Somente administradores podem usar esse comando");
          break;
        }
        var current_version = Updater.CurrentVersion(cfg);
        await bot.sendTextMesssageWraper(user.id, $"Versão atual do sistema chatbot: {current_version.ToString("yyyyMMdd")}");
        await bot.sendTextMesssageWraper(user.id, "Verificando se há novas versões do sistema chatbot...");
        var updates_list = Updater.ListUpdates(cfg);
        var update_version = Updater.HasUpdate(updates_list, current_version);
        if(update_version == null)
        {
          await bot.sendTextMesssageWraper(user.id, "A versão atual já é a versão mais recente!");
        }
        else
        {
          await bot.sendTextMesssageWraper(user.id, $"Nova versão {update_version} do sistema chatbot encontrada!");
          Updater.Restart(cfg);
        }
        break;
      case "/hotfix":
        if(user.has_privilege != UsersModel.userLevel.administrador)
        {
          await bot.sendTextMesssageWraper(user.id, "Somente administradores podem usar esse comando");
          break;
        }
        var version_filepath = System.IO.Path.Combine(cfg.CURRENT_PATH, "version");
        var version_fileinfo = new System.IO.FileInfo(version_filepath);
        var version_filediff = DateTime.Now - version_fileinfo.LastWriteTime;
        if(version_filediff.TotalMinutes > 5)
        {
          System.IO.File.WriteAllText(version_filepath, DateTime.MinValue.ToString("yyyyMMdd"));
          Updater.Restart(cfg);
          break;
        }
        await bot.sendTextMesssageWraper(user.id, "Sistema atualizado com sucesso!");
        break;
    }
    return;
  }
}