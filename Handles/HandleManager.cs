namespace telbot.handle;
using telbot.models;
public class HandleManager
{
  private HandleMessage bot;
  private Request request;
  private UsersModel user;
  private Configuration cfg;
  public HandleManager(HandleMessage bot, Configuration cfg, UsersModel user, Request request)
  {
    this.bot = bot;
    this.cfg = cfg;
    this.user = user;
    this.request = request;
  }
  async public Task routerManager()
  {
    switch (request.aplicacao)
    {
      case "autorizar": await autorizar(); break;
      case "atualizar": await autorizar(); break;
      case "promover": await promover(); break;
    }
    return;
  }
  async public Task autorizar()
  {
    if(!user.has_privilege)
    {
      await bot.sendTextMesssageWraper(user.id, "Você não tem permissão para alterar usuários!");
      Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, false));
      return;
    }
    if(Int64.TryParse(request.informacao, out long id))
    {
      try
      {
        Database.recuperarUsuario(id);
        if(Database.atualizarUsuario(id, user.id))
        {
          await bot.sendTextMesssageWraper(id, "Usuário atualizado com sucesso!");
          await bot.sendTextMesssageWraper(user.id, "Usuário atualizado com sucesso!");
          Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, true));
        }
        else
        {
          await bot.sendTextMesssageWraper(user.id, "Houve um problema em atualizar o usuário");
          await bot.sendTextMesssageWraper(user.id, "Verifique as informações e tente novamente");
          Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, false));
        }
      }
      catch
      {
        try
        {
          Database.inserirUsuario(new UsersModel(id, user.id));
          await bot.sendTextMesssageWraper(id, "Usuário autorizado com sucesso!");
          await bot.sendTextMesssageWraper(user.id, "Usuário autorizado com sucesso!");
          Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, true));
        }
        catch
        {
          await bot.sendTextMesssageWraper(user.id, "Houve um problema em autorizar o usuário");
          await bot.sendTextMesssageWraper(user.id, "Verifique as informações e tente novamente");
          Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, false));
        }
        return;
      }
    }
    else
    {
      await bot.sendTextMesssageWraper(user.id, "O identificador do usuário não é válido!");
      Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, false));
    }
    return;
  }
  async public Task promover()
  {
    if(user.id != cfg.ID_ADM_BOT)
    {
      await bot.sendTextMesssageWraper(user.id, "Você não tem permissão para promover usuários!");
      Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, false));
      return;
    }
    if(Int64.TryParse(request.informacao, out long id))
    {
      if(Database.promoverUsuario(id, user.id))
      {
        await bot.sendTextMesssageWraper(user.id, "Usuário promovido com sucesso!");
        Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, true));
      }
      else
      {
        await bot.sendTextMesssageWraper(user.id, "Houve um problema em promover o usuário");
        await bot.sendTextMesssageWraper(user.id, "Verifique as informações e tente novamente");
        Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, false));
      }
    }
    else
    {
      await bot.sendTextMesssageWraper(user.id, "O identificador do usuário não é válido!");
      Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, false));
    }
    return;
  }
}