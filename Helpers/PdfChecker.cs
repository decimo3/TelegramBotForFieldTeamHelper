namespace telbot.Helpers;
public static partial class PdfHandle
{
  public static Int64 Check(String filepath)
  {
    var text = new System.Text.StringBuilder();
    using var reader = new iTextSharp.text.pdf.PdfReader(filepath);
    var strategy = new iTextSharp.text.pdf.parser.SimpleTextExtractionStrategy();
    for (var page = 1; page <= reader.NumberOfPages; page++)
    {
        var currentText = iTextSharp.text.pdf.parser.PdfTextExtractor.GetTextFromPage(reader, page, strategy);
        text.Append(currentText);
    }
    var re = new System.Text.RegularExpressions.Regex("^[0-9]{10}$");
    var match = re.Match(text.ToString());
    return match.Success ? Convert.ToInt64(match.Value) : 0;
    }
}
