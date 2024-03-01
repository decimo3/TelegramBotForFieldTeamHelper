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
      case "promover": await promover(bot, cfg, user, request); break;
    }
    return;
  }
  public async static Task autorizar(HandleMessage bot, Configuration cfg, UsersModel user, Request request)
  {
    if(!user.has_privilege)
    {
      await bot.sendTextMesssageWraper(user.id, "Você não tem permissão para alterar usuários!");
      Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, false, request.received_at));
      return;
    }
    if(Int64.TryParse(request.informacao, out long id))
    {
      try
      {
        if(Database.recuperarUsuario(id) is null) throw new InvalidOperationException("O usuário não existe no banco de dados!");
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
      catch
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
        return;
      }
    }
    else
    {
      await bot.sendTextMesssageWraper(user.id, "O identificador do usuário não é válido!");
      Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, false, request.received_at));
    }
    return;
  }
  public async static Task promover(HandleMessage bot, Configuration cfg, UsersModel user, Request request)
  {
    if(user.id != cfg.ID_ADM_BOT)
    {
      await bot.sendTextMesssageWraper(user.id, "Você não tem permissão para promover usuários!");
      Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, false, request.received_at));
      return;
    }
    if(Int64.TryParse(request.informacao, out long id))
    {
      if(Database.promoverUsuario(id, user.id))
      {
        await bot.sendTextMesssageWraper(user.id, "Usuário promovido com sucesso!");
        Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, true, request.received_at));
      }
      else
      {
        await bot.sendTextMesssageWraper(user.id, "Houve um problema em promover o usuário");
        await bot.sendTextMesssageWraper(user.id, "Verifique as informações e tente novamente");
        Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, false, request.received_at));
      }
      return;
    }
    else
    {
      await bot.sendTextMesssageWraper(user.id, "O identificador do usuário não é válido!");
      Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, false, request.received_at));
    }
    return;
  }
}