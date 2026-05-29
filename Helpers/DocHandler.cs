using System.Text.RegularExpressions;
using telbot.handle;
using telbot.models;
using telbot.Services;

namespace telbot.Helpers;

public static class DocHandler
{
  private static List<DocsModel> _documents = new();
  private static readonly Regex _identifierRegex = new(@"^(?=.*[A-Z])(?=.*\d)[A-Z0-9]+$");
  public static async Task LoadDocs()
  {
    var database = Database.GetInstance();
    var adm_id = Configuration.GetInstance().ID_ADM_BOT;
    var baseDocs = database.RecuperarDocumento();
    var docsPath = Configuration.GetInstance().DOCS_PATH;
    var pathInfo = new System.IO.DirectoryInfo(docsPath);
    var pdfsInfo = pathInfo.GetFiles("*.pdf", System.IO.SearchOption.AllDirectories);
    var xlsxInfo = pathInfo.GetFiles("*.xlsx", System.IO.SearchOption.AllDirectories);
    var docsInfo = pdfsInfo.Concat(xlsxInfo).ToList();
    foreach (var docInfo in docsInfo)
    {
      if (System.IO.File.GetAttributes(docInfo.FullName).HasFlag(FileAttributes.Hidden))
        throw new InvalidOperationException(
          $"O arquivo {docInfo.FullName} está oculto!");
      var docInfoFileName = System.IO.Path.GetFileName(docInfo.FullName) ??
        throw new InvalidOperationException(
          $"O nome do arquivo {docInfo.FullName} não pode ser obtido!");
      var identifier = docInfoFileName.Split(' ').First();
      if (!_identifierRegex.IsMatch(identifier))
        throw new InvalidOperationException(
          $"Não pode obter o identificador válido para o arquivo {docInfo.FullName}!");
      var baseDoc = baseDocs.FirstOrDefault(b => b.filename.Equals(docInfoFileName, StringComparison.CurrentCultureIgnoreCase));
      if (baseDoc is null || docInfo.LastWriteTimeUtc.ToLocalTime() > baseDoc.updatedAt)
      {
        using var docStream = new System.IO.FileStream(docInfo.FullName, FileMode.Open, FileAccess.Read);
        var messageId = await HandleMessage.GetInstance()
          .SendDocumentAsyncWraper(adm_id, docStream, docInfoFileName) ??
            throw new InvalidOperationException(
              $"Não foi possível obter o ID do documento {docInfoFileName}!");
      // DONE - Case is not found, send to administrator and save on database
      if (baseDoc is null)
      {
        database.InserirDocumento(new DocsModel
        {
          messageId = messageId,
          identifier = identifier,
          filename = docInfoFileName,
          parent = docInfo.Directory?.Name,
          updatedAt = docInfo.LastWriteTimeUtc.ToLocalTime()
        });
        continue;
      }
      // DONE - Case the remote file is newer that database file, then update
      if (docInfo.LastWriteTimeUtc.ToLocalTime() > baseDoc.updatedAt)
      {
        baseDoc.messageId = messageId;
        baseDoc.updatedAt = docInfo.LastWriteTimeUtc.ToLocalTime();
        database.AlterarDocumento(baseDoc);
        continue;
      }
      }
    }
    // DONE - Remove information of obsolete instructions
    var delInfos = baseDocs
      .Where(b => !docsInfo.Any(d =>
        System.IO.Path.GetFileName(d.FullName)
        .Equals(b.filename, StringComparison.CurrentCultureIgnoreCase)))
      .ToList();
    foreach (var delInfo in delInfos)
    {
      delInfo.IsOutdated = true;
      database.AlterarDocumento(delInfo);
    }
    // DONE - retrieve updated documents information
    _documents = database.RecuperarDocumento();
  }
  public static DocsModel GetDocument(String text)
  {
    var doc = _documents.FirstOrDefault(d =>
      d.identifier.Equals(text, StringComparison.CurrentCultureIgnoreCase));
    if (doc is not null)
    {
      if(doc.IsOutdated)
        throw new InvalidOperationException(
          $"O documento {doc.filename} está obsoleto!");
      return doc;
    }
    var documents = _documents.Where(d => d.IsOutdated == false &&
      d.filename.Contains(text, StringComparison.CurrentCultureIgnoreCase))
        .Select(d => d.filename).ToList();
    throw new InvalidOperationException(
      "O documento solicitado não foi encontrado!\n\n" +
        (documents.Any() ? "Possíveis documentos relacionados:\n\n" +
          String.Join('\n', documents) : String.Empty));
  }
  public static List<DocsModel> GetDocuments()
  {
    var documents = _documents.Where(d => d.IsOutdated == false).ToList();
    if (!documents.Any())
      throw new InvalidOperationException($"Não foi encontrado nenhum documento!");
    return documents;
  }
}
