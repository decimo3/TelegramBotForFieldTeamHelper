using telbot.models;
using telbot.Helpers;
using telbot.Services;
namespace telbot.handle;
public static class HandleTypeMessage
{
  /// <summary>
  /// Retorna recursivamente todos os usuários descendentes
  /// do usuário informado na hierarquia de autorização.
  /// </summary>
  /// <param name="users">Lista completa de usuários.</param>
  /// <param name="identifier">Identificador do usuário raiz.</param>
  /// <returns>Usuários descendentes diretos e indiretos.</returns>
  private static IEnumerable<UsersModel> GetDescendants(List<UsersModel> users, long identifier)
  {
    var children = users
        .Where(x => x.inserted_by == identifier && x.identifier != identifier)
        .ToList();

    foreach (var child in children)
    {
        yield return child;

        foreach (var descendant in GetDescendants(users, child.identifier))
            yield return descendant;
    }
  }
  /// <summary>
  /// Retorna a lista de usuários visíveis ao usuário informado,
  /// de acordo com a hierarquia de autorização.
  /// </summary>
  /// <param name="usuario">
  /// Usuário para o qual a lista será obtida.
  /// </param>
  /// <returns>
  /// Lista de usuários permitidos pela hierarquia de autorização.
  /// </returns>
  /// <remarks>
  /// <list type="bullet">
  /// <item>
  /// <description>
  /// Se o usuário for <c>Proprietário</c>, retorna todos os usuários.
  /// </description>
  /// </item>
  /// <item>
  /// <description>
  /// Se o usuário for <c>Administrador</c>, retorna todos os usuários
  /// abaixo dele na hierarquia.
  /// </description>
  /// </item>
  /// <item>
  /// <description>
  /// Se o usuário for <c>Comunicador</c> autorizado por um
  /// <c>Proprietário</c>, retorna todos os usuários.
  /// </description>
  /// </item>
  /// <item>
  /// <description>
  /// Se o usuário for <c>Comunicador</c> autorizado por um
  /// <c>Administrador</c>, retorna o administrador autorizador e todos
  /// os usuários abaixo dele, excluindo o próprio comunicador.
  /// </description>
  /// </item>
  /// <item>
  /// <description>
  /// Nos demais casos, retorna apenas os usuários autorizados
  /// diretamente pelo usuário informado.
  /// </description>
  /// </item>
  /// </list>
  /// </remarks>
  private static List<UsersModel> GetUsers(UsersModel usuario)
  {
    var users = Database.GetInstance().RecuperarUsuario(u => u.dias_vencimento() > 0);
    // Proprietário
    if (usuario.privilege == UsersModel.userLevel.proprietario)
      return users;
    // Administrador
    if (usuario.privilege == UsersModel.userLevel.administrador)
      return GetDescendants(users, usuario.identifier).ToList();
    // Comunicador
    if (usuario.privilege == UsersModel.userLevel.comunicador)
    {
      var authorizer = users.First(x => x.identifier == usuario.inserted_by);
      // Comunicador autorizado pelo proprietário
      if (authorizer.privilege == UsersModel.userLevel.proprietario)
        return users;
      // Comunicador autorizado pelo administrador ou coordenador
      if (authorizer.privilege is UsersModel.userLevel.administrador or UsersModel.userLevel.coordenador)
        return GetDescendants(users, authorizer.identifier)
          .Where(x => x.identifier != usuario.identifier)
          .Append(authorizer)
          .ToList();
    }
    // Usuários autorizados diretamente pelo usuário
    return users.Where(x => x.inserted_by == usuario.identifier).ToList();
  }
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
    var usuarios = GetUsers(usuario);
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
      await bot.sendTextMesssageWraper(usuario.identifier, texto);
      await bot.SendCoordinateAsyncWraper(usuario.identifier, localizacao.Lat, localizacao.Lon);
    }
    await bot.sendTextMesssageWraper(usuario.identifier,
      "Créditos e agradecimento ao Jean Robocopy (4005767) pelas localizações dos equipamentos na regional oeste!");
    return;
  }
  public static async Task PhotographType(UsersModel usuario, DateTime recebido_em, String photograph, String? caption)
  {
    var chatbot = HandleMessage.GetInstance();
    if(!usuario.pode_transmitir())
    {
      await chatbot.sendTextMesssageWraper(
        usuario.identifier,
        "Você não possui permissão para enviar comunicados!");
      return;
    }
    var usuarios = GetUsers(usuario);
    caption = caption == null ? $"Enviado por: {usuario.username}" : caption + "\n\n" + $"Enviado por: {usuario.username}";
    await HandleAnnouncement.Comunicado(usuarios, usuario.identifier, caption, photograph, null, null);
    await HandleMessage.GetInstance().sendTextMesssageWraper(
      usuario.identifier,
      $"Comunicado enviado com sucesso para {usuarios.Count} usuários!");
    return;
  }
  public static async Task VideoclipType(UsersModel usuario, DateTime recebido_em, String videoclip, String? caption)
  {
    var chatbot = HandleMessage.GetInstance();
    if(!usuario.pode_transmitir())
    {
      await chatbot.sendTextMesssageWraper(
        usuario.identifier,
        "Você não possui permissão para enviar comunicados!");
      return;
    }
    var usuarios = GetUsers(usuario);
    caption = caption == null ? $"Enviado por: {usuario.username}" : caption + "\n\n" + $"Enviado por: {usuario.username}";
    await HandleAnnouncement.Comunicado(usuarios, usuario.identifier, caption, null, videoclip, null);
    await HandleMessage.GetInstance().sendTextMesssageWraper(
      usuario.identifier,
      $"Comunicado enviado com sucesso para {usuarios.Count} usuários!");
    return;
  }
  public static async Task DocumentType(UsersModel usuario, DateTime recebido_em, String document, String? caption)
  {
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
    var usuarios = GetUsers(usuario);
    caption = caption == null ? $"Enviado por: {usuario.username}" : caption + "\n\n" + $"Enviado por: {usuario.username}";
    await HandleAnnouncement.Comunicado(usuarios, usuario.identifier, caption, null, null, document);
    await HandleMessage.GetInstance().sendTextMesssageWraper(
      usuario.identifier,
      $"Comunicado enviado com sucesso para {usuarios.Count} usuários!");
    return;
  }
}