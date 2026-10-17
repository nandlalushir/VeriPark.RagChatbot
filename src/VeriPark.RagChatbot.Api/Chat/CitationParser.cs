using System.Text.RegularExpressions;
using VeriPark.RagChatbot.Api.Domain;

namespace VeriPark.RagChatbot.Api.Chat;

/// <summary>
/// Finds the "[p.5]" style page references the model wrote and matches them with the chunks
/// that were really retrieved. A page the model "cited" but never received is ignored,
/// so the citation list can only contain real passages from the document.
/// </summary>
public static partial class CitationParser
{
    private const int SnippetLength = 220;

    public static IReadOnlyList<Citation> Build(string answer, IReadOnlyList<RetrievedChunk> retrievedChunks)
    {
        var citedPages = ExtractPages(answer);
        if (citedPages.Count == 0)
        {
            return [];
        }

        return retrievedChunks
            .Where(chunk => citedPages.Contains(chunk.PageNumber))
            .OrderBy(chunk => chunk.PageNumber)
            .ThenByDescending(chunk => chunk.Score)
            .Select(chunk => new Citation(
                chunk.PageNumber,
                chunk.Id.ToString(),
                Math.Round(chunk.Score, 4),
                CreateSnippet(chunk.Text)))
            .ToList();
    }

    public static HashSet<int> ExtractPages(string answer)
    {
        var pages = new HashSet<int>();
        foreach (Match match in PageReferenceRegex().Matches(answer))
        {
            if (int.TryParse(match.Groups[1].Value, out var page))
            {
                pages.Add(page);
            }
        }

        return pages;
    }

    private static string CreateSnippet(string text)
    {
        var flat = WhitespaceRegex().Replace(text, " ").Trim();
        return flat.Length <= SnippetLength ? flat : flat[..SnippetLength] + "...";
    }

    [GeneratedRegex(@"(?<![A-Za-z])p\.\s?(\d{1,3})", RegexOptions.IgnoreCase)]
    private static partial Regex PageReferenceRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
