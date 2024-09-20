using telbot.models;
using telbot.Services;
namespace telbot.handle;
public static class HandleTypeMessage
{
  public static async Task ManuscriptsType(UsersModel usuario, DateTime recebido_em, String mensagem)
  {
    if(mensagem.Length < 50)
    {
      await HandleAsynchronous.Waiter(usuario.identifier, mensagem, recebido_em);
      return;
    }
    if(!usuario.pode_transmitir())
    {
      await HandleMessage.GetInstance().sendTextMesssageWraper(
        usuario.identifier,
        "Você não possui permissão para enviar comunicados!");
      return;
    }
    var usuarios = Database.GetInstance().RecuperarUsuario(u => u.dias_vencimento() > 0);
    var caption = "*COMUNICADO DO CHATBOT:*\n\n" + mensagem + $"\n\nEnviado por: {usuario.username}";
    await HandleAnnouncement.Comunicado(usuarios, usuario.identifier, caption, null, null, null);
    await HandleMessage.GetInstance().sendTextMesssageWraper(
      usuario.identifier,
      $"Comunicado enviado com sucesso para {usuarios.Count} usuários!");
    return;
  }
  public static async Task PhoneNumberType(UsersModel usuario, Int64 telefone, String username)
  {
    var database = Database.GetInstance();
    var chatbot = HandleMessage.GetInstance();
    usuario.phone_number = telefone;
    usuario.username = username;
    database.AlterarUsuario(usuario);
    await chatbot.RemoveRequest(usuario.identifier, telefone);
    return;
  }
  public static async Task CoordinatesType(UsersModel usuario, DateTime recebido_em, Double lat, Double lon)
  {
    var solicatacao = new logsModel()
    {
      identifier = usuario.identifier,
      application = "localizacao",
      received_at = recebido_em,
      typeRequest = TypeRequest.gpsInfo,
    };
    var bot = HandleMessage.GetInstance();
    var argumentos = new String[] {
      lat.ToString(System.Globalization.CultureInfo.InvariantCulture),
      lon.ToString(System.Globalization.CultureInfo.InvariantCulture),
      "--json"
    };
    var respostas = Executor.Executar("gps.exe", argumentos, true);
    if(String.IsNullOrEmpty(respostas))
    {
      await bot.ErrorReport(
        new InvalidOperationException(
          "Não foi recebida resposta do `GPS2ZNA`"),
        solicatacao);
      return;
    }
    var listaDeLocalizacoes = System.Text.Json.JsonSerializer.Deserialize<List<ZoneModel>>(respostas);
    if(listaDeLocalizacoes == null)
    {
      await bot.ErrorReport(
        new NullReferenceException(
          "Não foi recebida resposta do `GPS2ZNA`"),
        solicatacao);
      return;
    }
    foreach (var localizacao in listaDeLocalizacoes)
    {
      var texto = $"Zona: {localizacao.Nome} (~{Math.Round(localizacao.Mts)}mts)";
      var coord = localizacao.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture) + 
        "," + localizacao.Lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
      await bot.sendTextMesssageWraper(usuario.identifier, texto);
      await bot.SendCoordinateAsyncWraper(usuario.identifier, coord);
    }
    await bot.sendTextMesssageWraper(usuario.identifier,
      "Créditos e agradecimento ao Jean Robocopy (4005767) pelas localizações dos equipamentos na regional oeste!");
    return;
  }
  public static async Task PhotographType(UsersModel usuario, DateTime recebido_em, String photograph, String? caption)
  {
    var database = Database.GetInstance();
    var chatbot = HandleMessage.GetInstance();
    if(!usuario.pode_transmitir())
    {
      await chatbot.sendTextMesssageWraper(
        usuario.identifier,
        "Você não possui permissão para enviar comunicados!");
      return;
    }
    var usuarios = database.RecuperarUsuario(u => u.dias_vencimento() > 0);
    caption = caption == null ? $"Enviado por: {usuario.username}" : caption + "\n\n" + $"Enviado por: {usuario.username}";
    await HandleAnnouncement.Comunicado(usuarios, usuario.identifier, caption, photograph, null, null);
    await HandleMessage.GetInstance().sendTextMesssageWraper(
      usuario.identifier,
      $"Comunicado enviado com sucesso para {usuarios.Count} usuários!");
    return;
  }
  public static async Task VideoclipType(UsersModel usuario, DateTime recebido_em, String videoclip, String? caption)
  {
    var database = Database.GetInstance();
    var chatbot = HandleMessage.GetInstance();
    if(!usuario.pode_transmitir())
    {
      await chatbot.sendTextMesssageWraper(
        usuario.identifier,
        "Você não possui permissão para enviar comunicados!");
      return;
    }
    var usuarios = database.RecuperarUsuario(u => u.dias_vencimento() > 0);
    caption = caption == null ? $"Enviado por: {usuario.username}" : caption + "\n\n" + $"Enviado por: {usuario.username}";
    await HandleAnnouncement.Comunicado(usuarios, usuario.identifier, caption, null, videoclip, null);
    await HandleMessage.GetInstance().sendTextMesssageWraper(
      usuario.identifier,
      $"Comunicado enviado com sucesso para {usuarios.Count} usuários!");
    return;
  }
  public static async Task DocumentType(UsersModel usuario, DateTime recebido_em, String document, String? caption)
  {
    var database = Database.GetInstance();
    var chatbot = HandleMessage.GetInstance();
    if(!usuario.pode_transmitir())
    {
      await chatbot.sendTextMesssageWraper(
        usuario.identifier,
        "Você não possui permissão para enviar comunicados!");
      return;
    }
    if(String.IsNullOrEmpty(caption))
    {
      await chatbot.sendTextMesssageWraper(
        usuario.identifier,
        "Documentos necessitam de uma legenda descritiva!");
      return;
    }
    var usuarios = database.RecuperarUsuario(u => u.dias_vencimento() > 0);
    caption = caption == null ? $"Enviado por: {usuario.username}" : caption + "\n\n" + $"Enviado por: {usuario.username}";
    await HandleAnnouncement.Comunicado(usuarios, usuario.identifier, caption, null, null, document);
    await HandleMessage.GetInstance().sendTextMesssageWraper(
      usuario.identifier,
      $"Comunicado enviado com sucesso para {usuarios.Count} usuários!");
    return;
  }
}