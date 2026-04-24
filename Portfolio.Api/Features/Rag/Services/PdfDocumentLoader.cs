using UglyToad.PdfPig;

namespace Portfolio.Api.Features.Rag.Services;

public class PdfDocumentLoader
{
    private readonly ILogger<PdfDocumentLoader> _logger;

    public PdfDocumentLoader(ILogger<PdfDocumentLoader> logger)
    {
        _logger = logger;
    }

    public string LoadText(string filePath)
    {
        _logger.LogInformation("Loading PDF text. FilePath={FilePath}", filePath);

        using var document = PdfDocument.Open(filePath);
        var sb = new System.Text.StringBuilder();

        foreach (var page in document.GetPages())
        {
            sb.AppendLine(page.Text);
        }

        var text = sb.ToString();
        _logger.LogInformation(
            "PDF text loaded. FilePath={FilePath}, TextLength={TextLength}",
            filePath,
            text.Length);

        return text;
    }
}