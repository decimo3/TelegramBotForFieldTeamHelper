namespace telbot.handle;
using telbot.models;
public static class Manager
{
  public async static Task HandleManager(UsersModel user, logsModel request)
  {
    var database = Database.GetInstance();
    var bot = HandleMessage.GetInstance();
    if(!user.pode_autorizar())
    {
      await bot.sendTextMesssageWraper(user.identifier, "Você não tem permissão para alterar usuários!");
      return;
    }
    var usuario = database.RecuperarUsuario(request.information);
    if(usuario == null)
    {
      await bot.sendTextMesssageWraper(user.identifier, "Não há registro que esse usuário entrou em contato com o chatbot!");
      return;
    }
    if(usuario.privilege == UsersModel.userLevel.proprietario)
    {
      await bot.sendTextMesssageWraper(user.identifier, "Nenhum usuário tem permissão suficiente para alterar o proprietário!");
      return;
    }
    if(usuario.privilege == UsersModel.userLevel.administrador)
    {
      if(!user.pode_promover())
      {
        await bot.sendTextMesssageWraper(user.identifier, "Você não tem permissão suficiente para alterar o administrador!");
        return;
      }
    }
    usuario.update_at = request.received_at;
    switch (request.application)
    {
      case "autorizar":
      case "atualizar":
        if(usuario.dias_vencimento() > 99)
        {
          await bot.sendTextMesssageWraper(user.identifier, $"Usuário {usuario.identifier} com cargo {usuario.privilege} não tem prazo de expiração!");
          return;
        }
        if(usuario.privilege == UsersModel.userLevel.desautorizar)
          usuario.privilege = UsersModel.userLevel.eletricista;
      break;
      case "desautorizar":
        usuario.privilege = UsersModel.userLevel.desautorizar;
      break;
      case "controlador":
      case "comunicador":
      case "supervisor":
        if(!user.pode_promover())
        {
          await bot.sendTextMesssageWraper(user.identifier, "Você não tem permissão para alterar usuários!");
          return;
        }
        var cargo = Enum.Parse<UsersModel.userLevel>(request.application);
        usuario.privilege = cargo;
      break;
      case "administrador":
        if(user.privilege != UsersModel.userLevel.proprietario)
        {
          await bot.sendTextMesssageWraper(user.identifier, "Você não tem permissão para promover administradores!");
          return;
        }
          usuario.privilege = UsersModel.userLevel.administrador;
      break;
    }
    try
    {
      database.AlterarUsuario(usuario);
      await bot.sendTextMesssageWraper(user.identifier, "Usuário atualizado com sucesso!");
      await bot.sendTextMesssageWraper(usuario.identifier, "Usuário atualizado com sucesso!");
      if(usuario.phone_number == 0) await bot.RequestContact(usuario.identifier);
      request.status = 200;
      request.response_at = DateTime.Now;
      database.AlterarSolicitacao(request);
    }
    catch
    {
      await bot.sendTextMesssageWraper(user.identifier, "Houve um problema em atualizar o usuário");
      await bot.sendTextMesssageWraper(user.identifier, "Verifique as informações e tente novamente");
      request.status = 500;
      request.response_at = DateTime.Now;
      database.AlterarSolicitacao(request);
    }
    return;
  }
}
