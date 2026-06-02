using telbot.models;
using telbot.Services;
namespace telbot.handle;
public static class Manager
{
  public async static Task HandleManager(logsModel request, UsersModel user)
  {
    var database = Database.GetInstance();
    var bot = HandleMessage.GetInstance();
    if (user.identifier == request.information)
    {
      request.status = 400;
      var erro = new Exception($"Você não tem permissão para alterar o próprio cadastro!");
      await bot.ErrorReport(erro, request);
      return;
    }
    var usuario = database.RecuperarUsuario(request.information);
    if(usuario == null)
    {
      request.status = 404;
      var erro = new Exception("Não há registro que esse usuário entrou em contato com o chatbot!");
      await bot.ErrorReport(erro, request);
      return;
    }
    if (user.privilege <= usuario.privilege)
    {
      request.status = 401;
      var erro = new Exception("Você não tem permissão suficiente para alterar esse usuário!");
      await bot.ErrorReport(erro, request);
      return;
    }
    if (!Enum.TryParse<UsersModel.userLevel>(request.application, true, out var cargo))
    {
      request.status = 400;
      var cargos = String.Join(", ", Enum.GetNames(typeof(UsersModel.userLevel)));
      var erro = new Exception($"O cargo informado não é válido!\nLista de cargos: {cargos}");
      await bot.ErrorReport(erro, request);
      return;
    }
    if(!UsersModel.pode_autorizar(user.privilege, cargo))
    {
      request.status = 400;
      var erro = new Exception("Você não tem permissão para alterar esse usuário!");
      await bot.ErrorReport(erro, request);
      return;
    }
    usuario.inserted_by = request.identifier;
    usuario.update_at = request.received_at;
    usuario.privilege = cargo;
    try
    {
      database.AlterarUsuario(usuario);
      await bot.sendTextMesssageWraper(user.identifier, "Usuário atualizado com sucesso!");
      await bot.sendTextMesssageWraper(usuario.identifier, "Usuário atualizado com sucesso!");
      if(usuario.phone_number == 0) await bot.RequestContact(usuario.identifier);
      bot.SucessReport(request);
    }
    catch
    {
      request.status = 500;
      var erro = new Exception("Houve um problema em atualizar o usuário");
      await bot.ErrorReport(erro, request);
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
