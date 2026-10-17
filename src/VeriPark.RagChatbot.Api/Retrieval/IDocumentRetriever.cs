using VeriPark.RagChatbot.Api.Domain;

namespace VeriPark.RagChatbot.Api.Retrieval;

/// <summary>Finds the document chunks that are most relevant to a question.</summary>
public interface IDocumentRetriever
{
    Task<IReadOnlyList<RetrievedChunk>> RetrieveAsync(string query, CancellationToken cancellationToken);
}
