namespace telbot.handle;
using telbot.models;
public static class Manager
{
  public async static Task HandleManager(HandleMessage bot, Configuration cfg, UsersModel user, Request request)
  {
    switch (request.aplicacao)
    {
      case "autorizar": await autorizar(bot, cfg, user, request); break;
      case "atualizar": await autorizar(bot, cfg, user, request); break;
      case "monitorador": await promover(bot, cfg, user, request); break;
      case "comunicador": await promover(bot, cfg, user, request); break;
      case "administrador": await promover(bot, cfg, user, request); break;
      case "desautorizar": await promover(bot, cfg, user, request); break;
      case "supervisor": await promover(bot, cfg, user, request); break;
    }
    return;
  }
  public async static Task autorizar(HandleMessage bot, Configuration cfg, UsersModel user, Request request)
  {
    if(!user.pode_autorizar())
    {
      await bot.sendTextMesssageWraper(user.id, "Você não tem permissão para alterar usuários!");
      Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, false, request.received_at));
      return;
    }
    if(!Int64.TryParse(request.informacao, out long id))
    {
      await bot.sendTextMesssageWraper(user.id, "O identificador do usuário não é válido!");
      Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, false, request.received_at));
      return;
    }
    if(Database.recuperarUsuario(id) is null)
    {
      try
      {
        Database.inserirUsuario(new UsersModel(id, user.id));
        await bot.sendTextMesssageWraper(id, "Usuário autorizado com sucesso!");
        await bot.sendTextMesssageWraper(user.id, "Usuário autorizado com sucesso!");
        Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, true, request.received_at));
      }
      catch
      {
        await bot.sendTextMesssageWraper(user.id, "Houve um problema em autorizar o usuário");
        await bot.sendTextMesssageWraper(user.id, "Verifique as informações e tente novamente");
        Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, false, request.received_at));
      }
    }
    else
    {
      if(Database.atualizarUsuario(id, user.id))
      {
        await bot.sendTextMesssageWraper(id, "Usuário atualizado com sucesso!");
        await bot.sendTextMesssageWraper(user.id, "Usuário atualizado com sucesso!");
        Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, true, request.received_at));
      }
      else
      {
        await bot.sendTextMesssageWraper(user.id, "Houve um problema em atualizar o usuário");
        await bot.sendTextMesssageWraper(user.id, "Verifique as informações e tente novamente");
        Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, false, request.received_at));
      }
    }
  }
  public async static Task promover(HandleMessage bot, Configuration cfg, UsersModel user, Request request)
  {
    if(request.aplicacao == "administrador" && user.has_privilege != UsersModel.userLevel.proprietario)
    {
      await bot.sendTextMesssageWraper(user.id, "Você não tem permissão definir administradores do sistema!");
      Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, false, request.received_at));
      return;
    }
    if(!user.pode_promover())
    {
      await bot.sendTextMesssageWraper(user.id, "Você não tem permissão para modificar usuários!");
      Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, false, request.received_at));
      return;
    }
    if(!Int64.TryParse(request.informacao, out long id))
    {
      await bot.sendTextMesssageWraper(user.id, "O identificador do usuário não é válido!");
      Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, false, request.received_at));
      return;
    }
    if(Database.promoverUsuario(id, user.id, Enum.Parse<UsersModel.userLevel>(request.aplicacao)))
    {
      await bot.sendTextMesssageWraper(user.id, "Usuário alterado com sucesso!");
      Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, true, request.received_at));
    }
    else
    {
      await bot.sendTextMesssageWraper(user.id, "Houve um problema ao alterar o usuário");
      await bot.sendTextMesssageWraper(user.id, "Verifique as informações e tente novamente");
      Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, false, request.received_at));
    }
    return;
  }
}
