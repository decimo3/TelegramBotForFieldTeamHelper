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
  public async static Task<UsersModel?> HandleSecury(Int64 identificador, DateTime recebido_em)
  {
    var database = Database.GetInstance();
    var telegram = HandleMessage.GetInstance();
    //##################################################//
    //       Verifica se o usuário está cadastrado      //
    //       e se tem permissão para usar o sistema     //
    //##################################################//
    var user = database.RecuperarUsuario(identificador);
    if(user is null)
    {
      database.InserirUsuario(new UsersModel() {
        identifier = identificador,
        create_at = recebido_em,
        update_at = DateTime.Now
      });
      await telegram.sendTextMesssageWraper(identificador, "Seja bem vindo ao sistema de atendimento automático do chatbot!");
      await telegram.sendTextMesssageWraper(identificador, "Eu não estou autorizado a te passar informações no momento");
      await telegram.sendTextMesssageWraper(identificador, $"Seu identificador do Telegram é `{identificador}`.");
      await telegram.sendTextMesssageWraper(identificador, "Informe ao seu supervisor esse identificador para ter acesso");
      return null;
    }
    if(user.privilege == UsersModel.userLevel.desautorizar)
    {
      await telegram.sendTextMesssageWraper(identificador, "Eu não estou autorizado a te passar informações no momento");
      await telegram.sendTextMesssageWraper(identificador, "Para restaurar o seu acesso ao sistema, solicite ao seu supervisor o acesso ao BOT.");
      await telegram.sendTextMesssageWraper(identificador, $"Seu identificador do telegram é `{identificador}`, esse número deverá ser informado ao seu supervisor.");
      return null;
    }
    //##################################################//
    //       Verifica se o prazo da autorização de      //
    //     eletricistas, supervisores e controladores   //
    //##################################################//
    if(user.dias_vencimento() <= 0)
    {
      await telegram.sendTextMesssageWraper(identificador, "Sua autorização expirou e não posso mais te passar informações");
      await telegram.sendTextMesssageWraper(identificador, "Solicite a autorização novamente para o seu supervisor!");
      await telegram.sendTextMesssageWraper(identificador, $"Seu identificador do Telegram é `{identificador}`.");
      return null;
    }
    // Verifica se o cadastro está perto de expirar (7 dias antes) e avisa
    if(user.dias_vencimento() <= 7)
    {
      await telegram.sendTextMesssageWraper(identificador, "Sua autorização está quase expirando!");
      await telegram.sendTextMesssageWraper(identificador, "Solicite a **atualização** para o seu supervisor!");
      await telegram.sendTextMesssageWraper(identificador, $"Seu identificador do Telegram é `{identificador}`.");
    }
    return user;
  }
}
