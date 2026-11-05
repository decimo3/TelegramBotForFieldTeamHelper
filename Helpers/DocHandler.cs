using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using telbot.models;
using telbot.Services;

namespace telbot.Helpers;

public class DocHandler
{
  private static Boolean is_ready = false;
  private static List<DocsModel> _documents = new();
  private static readonly Regex _identifierRegex = new(@"^[A-Z0-9]{4,12}$");
  public static async void LoadDocs()
  {
    var logger = Logger.GetInstance<DocHandler>();
    var filepaths = Configuration.GetInstance().DOCS_PATH.Split(',');
    foreach (var filepath in filepaths)
    {
      if (!System.IO.Directory.Exists(filepath))
        throw new InvalidOperationException(
          $"A pasta {filepath} informada em DOCS_PATH está inalcançavel!");

      var files = new System.IO.DirectoryInfo(filepath).GetFiles("*", SearchOption.AllDirectories);
      foreach (var file in files)
      {
        if (file.Attributes.HasFlag(FileAttributes.Hidden))
        {
          logger.LogWarning("O arquivo {filename} está oculto! Pulando...", file.FullName);
          continue;
        }
        var identifier = (file.Name.Contains(' ') ? file.Name.Split(' ', 2)[0] : file.Name).Replace(file.Extension, "");
        if (!_identifierRegex.IsMatch(identifier))
        {
          logger.LogWarning("O identificador do arquivo {filename} é inválido!", file.FullName);
          continue;
        }
        _documents.Add(new DocsModel
        {
          messageId = String.Empty,
          identifier = identifier,
          filename = file.Name,
          parent = file.FullName,
          updatedAt = file.LastWriteTimeUtc.Date
        });
      }
    }
    logger.LogInformation("Os documentos já foram indexados!");
    is_ready = true;
  }
  public static List<DocsModel> GetDocuments(String text)
  {
    if (!is_ready) throw new InvalidOperationException(
        "O sistema não terminou de realizar a indexação, aguarde e solicite em breve!");
    var docs = _documents.Where(d => d.IsOutdated == false &&
      d.identifier.Equals(text, StringComparison.OrdinalIgnoreCase)).ToList();
    if (!docs.Any())
    {
      var related = GetDocList(text);
      throw new InvalidOperationException(
        "O documento solicitado não foi encontrado!\n\n" +
          (related.Any() ? "Possíveis documentos relacionados:\n\n" +
            String.Join('\n', related) : String.Empty));
    }
    return docs;
  }
  public static List<String> GetDocList(string text)
  {
    if (!is_ready) throw new InvalidOperationException(
        "O sistema não terminou de realizar a indexação, aguarde e solicite em breve!");

    var agrupamentos = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) { "INDICA", "ENEL", "LIGHT" };

    var documents = agrupamentos.Contains(text) ?
        _documents.Where(d => d.parent.Contains(text, StringComparison.InvariantCultureIgnoreCase)) :
        _documents.Where(d => d.filename.Contains(text, StringComparison.InvariantCultureIgnoreCase));

    if (!documents.Any()) throw new InvalidOperationException(
        "Não foi encontrado nenhum documento com o critério informado!");

    return documents
      .GroupBy(d => new FileInfo(d.parent).Directory?.Name ?? "Origem desconhecida!")
      .Select(g => $"{g.Key}:\n\n" + string.Join('\n', g.Select(d => d.filename)))
      .ToList();
  }
}
