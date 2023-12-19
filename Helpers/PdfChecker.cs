namespace telbot;
public static class PdfChecker
{
  public static bool PdfCheck(String filepath, String instalacao)
  {
    try
    {
      if(!System.IO.File.Exists(filepath)) return false;
      var reader = new iTextSharp.text.pdf.PdfReader(filepath);
      var text = new System.Text.StringBuilder();
      for (int page = 1; page <= reader.NumberOfPages; page++) {
        var strategy = new iTextSharp.text.pdf.parser.SimpleTextExtractionStrategy();
        string currentText = iTextSharp.text.pdf.parser.PdfTextExtractor.GetTextFromPage(reader, page, strategy);
        currentText = System.Text.Encoding.UTF8.GetString(System.Text.ASCIIEncoding.Convert(
          System.Text.Encoding.Default, System.Text.Encoding.UTF8, System.Text.Encoding.Default.GetBytes(currentText)));
        text.Append(currentText);
      }
      reader.Close();
      String result = text.ToString();
      return result.Contains(instalacao);
    }
    catch
    {
      return false;
    }
  }
}
