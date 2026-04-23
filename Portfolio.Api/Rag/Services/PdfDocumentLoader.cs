using UglyToad.PdfPig;

public class PdfDocumentLoader
{
    public string LoadText(string filePath)
    {
        using var document = PdfDocument.Open(filePath);
        var sb = new System.Text.StringBuilder();

        foreach (var page in document.GetPages())
        {
            sb.AppendLine(page.Text);
        }

        return sb.ToString();
    }
}