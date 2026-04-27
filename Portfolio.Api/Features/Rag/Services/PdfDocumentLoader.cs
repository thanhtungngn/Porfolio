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
        return ReadDocumentText(document, filePath);
    }

    public string LoadText(Stream stream, string sourceName)
    {
        _logger.LogInformation("Loading PDF text. Source={Source}", sourceName);

        using var document = PdfDocument.Open(stream);
        return ReadDocumentText(document, sourceName);
    }

    private string ReadDocumentText(PdfDocument document, string sourceName)
    {
        var sb = new System.Text.StringBuilder();

        foreach (var page in document.GetPages())
        {
            sb.AppendLine(page.Text);
        }

        var text = sb.ToString();
        _logger.LogInformation(
            "PDF text loaded. Source={Source}, TextLength={TextLength}",
            sourceName,
            text.Length);

        return text;
    }
}