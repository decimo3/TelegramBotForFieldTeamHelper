using telbot.Services;
namespace telbot.handle;
public static class HandleTypeMessage
{
  public static async Task ManuscriptsType(UsersModel usuario, DateTime recebido_em, String mensagem)
  {
    await HandleAsynchronous.Soiree(usuario.identifier, mensagem, recebido_em);
  }
  public static async Task PhoneNumberType(UsersModel usuario, Int64 telefone)
  {
    var database = Database.GetInstance();
    var chatbot = HandleMessage.GetInstance();
    usuario.phone_number = telefone;
    database.AlterarUsuario(usuario);
    await chatbot.RemoveRequest(usuario.identifier, telefone);
    return;
  }
  public static async Task CoordinatesType(UsersModel usuario, DateTime recebido_em, Double lat, Double lon)
  {
    var bot = HandleMessage.GetInstance();
    var argumentos = new String[] {
      lat.ToString(System.Globalization.CultureInfo.InvariantCulture),
      lon.ToString(System.Globalization.CultureInfo.InvariantCulture),
    };
    var respostas = Executor.Executar("gps.exe", argumentos, true);
    await bot.sendTextMesssageWraper(usuario.identifier, String.Join("\n", respostas));
    return;
  }
  public static async Task PhotographType(UsersModel usuario, DateTime recebido_em, String photograph, String? caption)
  {
    var database = Database.GetInstance();
    var chatbot = HandleMessage.GetInstance();
    if(!usuario.pode_transmitir())
    {
      await chatbot.sendTextMesssageWraper(usuario.identifier, "Você não possui permissão para enviar comunicados!");
      return;
    }
    var usuarios = database.RecuperarUsuario(u => u.dias_vencimento() > 0);
    // usuarios = usuarios.Where(u => u.dias_vencimento() > 0).ToList();
    await HandleAnnouncement.Comunicado(usuarios, usuario.identifier, caption, photograph, null, null);
    return;
  }
  public static async Task VideoclipType(UsersModel usuario, DateTime recebido_em, String videoclip, String? caption)
  {
    var database = Database.GetInstance();
    var chatbot = HandleMessage.GetInstance();
    if(!usuario.pode_transmitir())
    {
      await chatbot.sendTextMesssageWraper(usuario.identifier, "Você não possui permissão para enviar comunicados!");
      return;
    }
    var usuarios = database.RecuperarUsuario(u => u.dias_vencimento() > 0);
    // usuarios = usuarios.Where(u => u.dias_vencimento() > 0).ToList();
    await HandleAnnouncement.Comunicado(usuarios, usuario.identifier, caption, null, videoclip, null);
    return;
  }
  public static async Task DocumentType(UsersModel usuario, DateTime recebido_em, String document, String? caption)
  {
    var database = Database.GetInstance();
    var chatbot = HandleMessage.GetInstance();
    if(!usuario.pode_transmitir())
    {
      await chatbot.sendTextMesssageWraper(usuario.identifier, "Você não possui permissão para enviar comunicados!");
      return;
    }
    var usuarios = database.RecuperarUsuario(u => u.dias_vencimento() > 0);
    // usuarios = usuarios.Where(u => u.dias_vencimento() > 0).ToList();
    await HandleAnnouncement.Comunicado(usuarios, usuario.identifier, caption, null, null, document);
    return;
  }
}