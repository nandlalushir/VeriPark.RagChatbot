using System.Security.Cryptography;
using System.Text;

namespace VeriPark.RagChatbot.Api.Ingestion;

/// <summary>
/// Creates the same Guid for the same input every time.
/// Qdrant needs a Guid or number as point id; using a stable id means re-running ingestion
/// overwrites existing points instead of creating duplicates.
/// </summary>
public static class DeterministicGuid
{
    public static Guid Create(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return new Guid(hash.AsSpan(0, 16));
    }
}
