namespace telbot.handle;
using telbot.models;
public static class Manager
{
  public async static Task HandleManager(HandleMessage bot, Configuration cfg, UsersModel user, Request request)
  {
    if(!user.pode_autorizar())
    {
      await bot.sendTextMesssageWraper(user.id, "Você não tem permissão para alterar usuários!");
      return;
    }
    if(!Int64.TryParse(request.informacao, out long id))
    {
      await bot.sendTextMesssageWraper(user.id, "O identificador do usuário não é válido!");
      return;
    }
    var usuario = Database.recuperarUsuario(id);
    if(usuario == null)
    {
      await bot.sendTextMesssageWraper(user.id, "Não há registro que esse usuário entrou em contato com o chatbot!");
      return;
    }
    if(usuario.has_privilege == UsersModel.userLevel.proprietario)
    {
      await bot.sendTextMesssageWraper(user.id, "Nenhum usuário tem permissão suficiente para alterar o proprietário!");
      return;
    }
    if(usuario.has_privilege == UsersModel.userLevel.administrador)
    {
      if(!user.pode_promover())
      {
        await bot.sendTextMesssageWraper(user.id, "Você não tem permissão suficiente para alterar o administrador!");
        return;
      }
    }
    var cargo = Enum.Parse<UsersModel.userLevel>(request.aplicacao!);
    usuario.has_privilege = cargo;
    usuario.update_at = DateTime.Now;
    switch (request.aplicacao)
    {
      case "autorizar":
      case "atualizar":
      case "desautorizar":
        await alterarUsuario(bot, user, usuario, request);
      break;
      case "monitorador":
      case "comunicador":
      case "supervisor":
        if(!user.pode_promover())
          await bot.sendTextMesssageWraper(user.id, "Você não tem permissão para alterar usuários!");
        else
          await alterarUsuario(bot, user, usuario, request);
      break;
      case "administrador":
        if(user.has_privilege != UsersModel.userLevel.proprietario)
          await bot.sendTextMesssageWraper(user.id, "Você não tem permissão para promover administradores!");
        else
          await alterarUsuario(bot, user, usuario, request);
      break;
    }
    return;
  }
  private async static Task alterarUsuario(HandleMessage bot, UsersModel old_user, UsersModel new_user, Request request)
  {
    try
    {
      Database.alterarUsuario(new_user, old_user.id);
      await bot.sendTextMesssageWraper(old_user.id, "Usuário atualizado com sucesso!");
      await bot.sendTextMesssageWraper(new_user.id, "Usuário atualizado com sucesso!");
      Database.inserirRelatorio(new logsModel(old_user.id, request.aplicacao, request.informacao, true, request.received_at));
    }
    catch
    {
      await bot.sendTextMesssageWraper(old_user.id, "Houve um problema em atualizar o usuário");
      await bot.sendTextMesssageWraper(old_user.id, "Verifique as informações e tente novamente");
      Database.inserirRelatorio(new logsModel(old_user.id, request.aplicacao, request.informacao, false, request.received_at));
    }
  }
}
