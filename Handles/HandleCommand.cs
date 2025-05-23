using telbot.Helpers;
using telbot.models;
using telbot.Services;
namespace telbot.handle;
public static class Command
{
  public async static Task HandleCommand(logsModel request, UsersModel user)
  {
    var bot = HandleMessage.GetInstance();
    var cfg = Configuration.GetInstance();
    try
    {
    switch (request.application)
    {
      case "/start":
        await bot.sendTextMesssageWraper(user.identifier, "Seja bem vindo ao programa de automação de respostas do MestreRuan");
        await bot.sendTextMesssageWraper(user.identifier, "Se tiver em dúvida de como usar o bot, digite /ajuda.");
        break;
      case "/ajuda":
        await bot.sendTextMesssageWraper(user.identifier, "Para consultas de informações, digite a aplicação e depois o número da nota ou instalação.");
        await bot.sendTextMesssageWraper(user.identifier, "*TELEFONE* ou *CONTATO* para receber todos os telefones no cadastro do cliente;");
        await bot.sendTextMesssageWraper(user.identifier, "*COORDENADA* para receber um link da localização no cadastro do cliente;");
        await bot.sendTextMesssageWraper(user.identifier, "*ROTEIRO* para receber a lista de instalações ordenada por horário;");
        await bot.sendTextMesssageWraper(user.identifier, "*LEITURISTA* para receber a lista de instalações ordenada por sequencia;");
        await bot.sendTextMesssageWraper(user.identifier, "*PENDENTE* para receber a lista de débitos para aquela instalação do cliente;");
        await bot.sendTextMesssageWraper(user.identifier, "*FATURA* ou *DEBITO* _(sem acentuação)_ para receber as faturas vencidas em PDF (limite de 5 faturas)");
        await bot.sendTextMesssageWraper(user.identifier, "*HISTORICO* _(sem acentuação)_ para receber a lista com os 5 últimos serviços para a instalação;");
        await bot.sendTextMesssageWraper(user.identifier, "*AGRUPAMENTO* para receber as informações referentes ao PC;");
        await bot.sendTextMesssageWraper(user.identifier, "*CRUZAMENTO* para receber as ruas que cruzam com o logradouro da nota;");
        await bot.sendTextMesssageWraper(user.identifier, "*CONSUMO* para receber informações das últimas leituras e consumos do cliente;");
        await bot.sendTextMesssageWraper(user.identifier, "*ABERTURA* para receber o resultado da análise automática da instalação para abertura de nota de recuperação;");
        await bot.sendTextMesssageWraper(user.identifier, "*REN360* para receber a lista dos consumos dos clientes próximos e passividade para abertura de nota de recuperação;");
        await bot.sendTextMesssageWraper(user.identifier, "*EVIDENCIA* para receber as informações de finalização de notas no OFS");
        await bot.sendTextMesssageWraper(user.identifier, "*FUGA* para receber a lista de instalações para o mesmo número de rua com seus devidos débitos");
        await bot.sendTextMesssageWraper(user.identifier, "*CLIENTE* para receber informações de instalações para o CPF/CNPJ;");
        await bot.sendTextMesssageWraper(user.identifier, "*SERVICO* para receber informações da ordem de serviço;");
        await bot.sendTextMesssageWraper(user.identifier, "*INSTALACAO* para receber informações da instalação;");
        await bot.sendTextMesssageWraper(user.identifier, "*MEDIDOR* para receber as informações referentes ao medidor;");
        await bot.sendTextMesssageWraper(user.identifier, "*INFORMACAO* para receber informações gerais da instalacao;");
        if(user.pode_relatorios())
        {
          await bot.sendTextMesssageWraper(user.identifier, "*RELIGACAO* para receber o relatório de notas de RELIGA em aberto no sistema;");
          await bot.sendTextMesssageWraper(user.identifier, "*LIDEANEXO* para receber o relatório de notas de LIDE e ANEXO IV em aberto no sistema;");
          await bot.sendTextMesssageWraper(user.identifier, "*BANDEIRADA* para receber o relatório de notas de AFERICAO;");
        }
        if(user.pode_autorizar())
        {
          await bot.sendTextMesssageWraper(user.identifier, "*EVIDENCIA* para receber as informações de finalização de notas no OFS");
          await bot.sendTextMesssageWraper(user.identifier, "Para os comandos de gestão, digite o cargo e depois insira o número do identificador");
          await bot.sendTextMesssageWraper(user.identifier, "*AUTORIZAR* para cadastrar novos usuários com acesso de consulta no sistema chatbot");
          await bot.sendTextMesssageWraper(user.identifier, "*ATUALIZAR* para renovar o prazo de expiração um usuário com acesso de consulta do sistema");
          await bot.sendTextMesssageWraper(user.identifier, "*SUPERVISOR* para alterar para um usuário que pode autorizar outros usuários");
          await bot.sendTextMesssageWraper(user.identifier, "*DESAUTORIZAR* para remover o acesso de consulta de um usuário no sistema chatbot");
          await bot.sendTextMesssageWraper(user.identifier, "*CONTROLADOR* para alterar para um usuário que pode receber avisos sobre outros usuário");
          await bot.sendTextMesssageWraper(user.identifier, "*COMUNICADOR* para alterar para um usuário capaz de enviar transmissões pelo sistema");
        }
        await bot.sendTextMesssageWraper(user.identifier, "Todas as solicitações não possuem acentuação e são no sigular (não tem o 's' no final).");
        break;
      case "/info":
        {
        var info = new System.Text.StringBuilder();
        info.Append($"*Identificador:* {user.identifier}\n");
        info.Append($"*Telefone:* {user.phone_number}\n");
        info.Append($"*Autorização:* {user.privilege.ToString()}\n");
        if(user.dias_vencimento() < 99)
        {
          info.Append($"*Expiração:* {user.update_at.AddDays(user.dias_vencimento())} ({user.dias_vencimento()} dias)\n");
        }
        info.Append($"*Versão:* {Updater.CurrentVersion().ToString("yyyyMMdd")}");
        await bot.sendTextMesssageWraper(user.identifier, info.ToString());
        }
        break;
      case "/update":
        if(user.privilege != UsersModel.userLevel.proprietario)
        {
          request.status = 403;
          throw new InvalidOperationException("Somente o proprietario podem usar esse comando!");
        }
        try
        {
        var current_version = Updater.CurrentVersion();
        await bot.sendTextMesssageWraper(user.identifier, $"Versão atual do sistema chatbot: {current_version.ToString("yyyyMMdd")}");
        await bot.sendTextMesssageWraper(user.identifier, "Verificando se há novas versões do sistema chatbot...");
        var updates_list = Updater.ListUpdates();
        var update_version = Updater.HasUpdate(updates_list, current_version);
        if(update_version == null)
        {
          await bot.sendTextMesssageWraper(user.identifier, "A versão atual já é a versão mais recente!");
          break;
        }
        else
        {
          await bot.sendTextMesssageWraper(user.identifier, $"Nova versão {update_version} do sistema chatbot encontrada!");
          Updater.Restart();
        }
        }
        catch
        {
          request.status = 500;
          throw new InvalidOperationException("Não foi possível atualizar o sistema remotamente!");
        }
        break;
      case "/hotfix":
        if(user.privilege != UsersModel.userLevel.proprietario)
        {
          request.status = 403;
          throw new InvalidOperationException("Somente o proprietario podem usar esse comando!");
        }
        if(!Updater.IsChangedVersionFile())
        {
          Updater.UpdateVersionFile(DateTime.MinValue);
          Updater.Restart();
          break;
        }
        await bot.sendTextMesssageWraper(user.identifier, "Sistema atualizado com sucesso!");
        break;
      case "/restart":
        if(!user.pode_promover())
        {
          request.status = 403;
          throw new InvalidOperationException("Somente o proprietario ou administrador podem usar esse comando!");
        }
        if(Updater.IsChangedVersionFile())
        {
          await bot.sendTextMesssageWraper(user.identifier, "Sistema reiniciado com sucesso!");
          break;
        }
        Updater.TerminateAll();
        await bot.sendTextMesssageWraper(user.identifier, "Processos finalizados, reiniciando o sistema...");
        Updater.UpdateVersionFile(Updater.CurrentVersion());
        Updater.Restart();
        break;
      case "/database":
        if(user.privilege != UsersModel.userLevel.proprietario)
        {
          request.status = 403;
          throw new InvalidOperationException("Somente o proprietario podem usar esse comando!");
        }
        var solicitacoes = Database.GetInstance().RecuperarSolicitacao();
        var tabela_texto = TableMaker<logsModel>.Serialize(solicitacoes, ';');
        Stream tabela_stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(tabela_texto));
        await bot.SendDocumentAsyncWraper(user.identifier, tabela_stream, $"{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.csv");
        tabela_stream.Close();
      break;
      case "/cmdargs":
      {
        var argumentos = System.Environment.GetCommandLineArgs();
        await bot.sendTextMesssageWraper(user.identifier, String.Join('\n', argumentos));
      }
      break;
      case "/configs":
      {
        var properties = typeof(Configuration).GetFields();
        var stringbuilder = new System.Text.StringBuilder();
        foreach (var property in properties)
        {
          stringbuilder.Append(property.Name);
          stringbuilder.Append(": ");
          stringbuilder.Append(property.GetValue(cfg));
          stringbuilder.Append('\n');
        }
        await bot.sendTextMesssageWraper(user.identifier, stringbuilder.ToString(), markdown: false);
      }
      break;
      default:
      {
        request.status = 400;
        throw new InvalidOperationException("Comando solicitado não foi programado!");
      }
    }
    bot.SucessReport(request);
    }
    catch (System.Exception error)
    {
      await bot.ErrorReport(error, request);
    }
  }
}