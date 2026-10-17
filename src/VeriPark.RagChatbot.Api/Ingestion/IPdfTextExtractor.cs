using VeriPark.RagChatbot.Api.Domain;

namespace VeriPark.RagChatbot.Api.Ingestion;

/// <summary>Reads a PDF file and returns its text page by page.</summary>
public interface IPdfTextExtractor
{
    IReadOnlyList<PageText> Extract(string pdfPath);
}
