using System.Text.RegularExpressions;
namespace telbot.Helpers;
public partial class PdfHandle
{
  private const string CPF_REGEX = @"\d{1,3}(?:\.\d{3}){2}\.\d{3}-\d{2}";
  private static Int64 Check(String filepath)
  {
    using var reader = new iTextSharp.text.pdf.PdfReader(filepath);
    var re = new Regex(CPF_REGEX);
    var strategy = new iTextSharp.text.pdf.parser.SimpleTextExtractionStrategy();
    for (var page = 1; page <= reader.NumberOfPages; page++)
    {
        var currentText = iTextSharp.text.pdf.parser.PdfTextExtractor.GetTextFromPage(reader, page, strategy);
        var lines = currentText.Split('\n');
        foreach (var line in lines)
        {
          var match = re.Match(line);
          if(match.Success)
          {
            // Clear output removing dots and dashes
            return Int64.Parse(Regex.Replace(match.Value, @"\D", ""));
          }
        }
    }
    return 0;
  }
}
