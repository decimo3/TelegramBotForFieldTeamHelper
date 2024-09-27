namespace telbot.Helpers;
public partial class PdfHandle
{
  public static Int64 Check(String filepath)
  {
    using var reader = new iTextSharp.text.pdf.PdfReader(filepath);
    var re = new System.Text.RegularExpressions.Regex("^[0-9]{10}$");
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
            return Int64.Parse(match.Value);
          }
        }
    }
    return 0;
  }
}
