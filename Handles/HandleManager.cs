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
    var usuario = Database.recuperarUsuario(request.informacao);
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
    usuario.update_at = request.received_at;
    switch (request.aplicacao)
    {
      case "autorizar":
      case "atualizar":
        if((int)usuario.has_privilege > 1)
        {
          await bot.sendTextMesssageWraper(user.id, $"Usuário {usuario.id} com acesso de {usuario.has_privilege.ToString()} não tem prazo de expiração!");
        }
        else
        {
          if(usuario.has_privilege == UsersModel.userLevel.desautorizar)
          {
            usuario.has_privilege = UsersModel.userLevel.eletricista;
          }
          await alterarUsuario(bot, user, usuario, request);
          
        }
      break;
      case "desautorizar":
        usuario.has_privilege = UsersModel.userLevel.desautorizar;
        await alterarUsuario(bot, user, usuario, request);
      break;
      case "controlador":
      case "comunicador":
      case "supervisor":
        var cargo = Enum.Parse<UsersModel.userLevel>(request.aplicacao!);
        usuario.has_privilege = cargo;
        if(!user.pode_promover())
          await bot.sendTextMesssageWraper(user.id, "Você não tem permissão para alterar usuários!");
        else
          await alterarUsuario(bot, user, usuario, request);
      break;
      case "administrador":
        usuario.has_privilege = UsersModel.userLevel.administrador;
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
      if(new_user.phone_number == 0) await bot.RequestContact(new_user.id);
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
