using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using VeriPark.RagChatbot.Api.Configuration;
using VeriPark.RagChatbot.Api.Domain;

namespace VeriPark.RagChatbot.Api.Ingestion;

/// <summary>
/// Recursive, page-aware text splitter.
/// <list type="number">
/// <item>Chunks never cross a page boundary, so every chunk has exactly one page number to cite.</item>
/// <item>A page that fits into <c>ChunkSize</c> stays in one piece.</item>
/// <item>Longer pages are split at the most natural boundary first: paragraph, then line, then sentence, then word.</item>
/// <item>Small pieces are merged back together up to <c>ChunkSize</c>, and neighbouring chunks share
/// <c>ChunkOverlap</c> characters so an idea that sits on a boundary is not lost.</item>
/// </list>
/// </summary>
public sealed class RecursiveTextChunker : IDocumentChunker
{
    private sealed record Separator(Regex Pattern, string Joiner);

    private static readonly Separator[] Separators =
    [
        new(new Regex(@"\n{2,}", RegexOptions.Compiled), "\n\n"),
        new(new Regex(@"\n", RegexOptions.Compiled), "\n"),
        new(new Regex(@"(?<=[.!?;:])\s+", RegexOptions.Compiled), " "),
        new(new Regex(@"\s+", RegexOptions.Compiled), " "),
    ];

    private readonly int _chunkSize;
    private readonly int _chunkOverlap;

    public RecursiveTextChunker(IOptions<RagOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _chunkSize = options.Value.ChunkSize;
        _chunkOverlap = options.Value.ChunkOverlap;

        if (_chunkOverlap >= _chunkSize)
        {
            throw new ArgumentException("Chunk overlap must be smaller than chunk size.", nameof(options));
        }
    }

    public IReadOnlyList<TextChunk> Chunk(string documentName, IReadOnlyList<PageText> pages)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentName);
        ArgumentNullException.ThrowIfNull(pages);

        var chunks = new List<TextChunk>();
        var chunkIndex = 0;

        foreach (var page in pages)
        {
            var text = NormalizeWhitespace(page.Text);
            if (text.Length == 0)
            {
                continue;
            }

            foreach (var piece in Split(text, level: 0))
            {
                var trimmed = piece.Trim();
                if (trimmed.Length > 0)
                {
                    chunks.Add(new TextChunk(documentName, page.PageNumber, chunkIndex++, trimmed));
                }
            }
        }

        return chunks;
    }

    private List<string> Split(string text, int level)
    {
        if (text.Length <= _chunkSize)
        {
            return [text];
        }

        if (level >= Separators.Length)
        {
            return HardSplit(text);
        }

        var separator = Separators[level];
        if (!separator.Pattern.IsMatch(text))
        {
            return Split(text, level + 1);
        }

        var pieces = new List<string>();
        foreach (var part in separator.Pattern.Split(text))
        {
            if (part.Length == 0)
            {
                continue;
            }

            if (part.Length <= _chunkSize)
            {
                pieces.Add(part);
            }
            else
            {
                pieces.AddRange(Split(part, level + 1));
            }
        }

        return Merge(pieces, separator.Joiner);
    }

    /// <summary>Glues small pieces together until the chunk is full, carrying a tail over as overlap.</summary>
    private List<string> Merge(List<string> pieces, string joiner)
    {
        var chunks = new List<string>();
        var current = new List<string>();

        foreach (var piece in pieces)
        {
            var wouldOverflow = current.Count > 0
                && JoinedLength(current, joiner) + joiner.Length + piece.Length > _chunkSize;

            if (wouldOverflow)
            {
                chunks.Add(string.Join(joiner, current));

                // Keep only the tail that fits inside the overlap budget (and leaves room for the new piece).
                while (current.Count > 0
                    && (JoinedLength(current, joiner) > _chunkOverlap
                        || JoinedLength(current, joiner) + joiner.Length + piece.Length > _chunkSize))
                {
                    current.RemoveAt(0);
                }
            }

            current.Add(piece);
        }

        if (current.Count > 0)
        {
            chunks.Add(string.Join(joiner, current));
        }

        return chunks;
    }

    /// <summary>Last resort for a single "word" that is longer than a whole chunk (for example a long URL).</summary>
    private List<string> HardSplit(string text)
    {
        var result = new List<string>();
        var step = _chunkSize - _chunkOverlap;

        for (var start = 0; start < text.Length; start += step)
        {
            var length = Math.Min(_chunkSize, text.Length - start);
            result.Add(text.Substring(start, length));

            if (start + length >= text.Length)
            {
                break;
            }
        }

        return result;
    }

    private static int JoinedLength(List<string> parts, string joiner)
    {
        if (parts.Count == 0)
        {
            return 0;
        }

        var total = joiner.Length * (parts.Count - 1);
        foreach (var part in parts)
        {
            total += part.Length;
        }

        return total;
    }

    private static string NormalizeWhitespace(string text)
    {
        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        normalized = Regex.Replace(normalized, @"[ \t]+", " ");
        normalized = Regex.Replace(normalized, @" ?\n ?", "\n");
        normalized = Regex.Replace(normalized, @"\n{3,}", "\n\n");
        return normalized.Trim();
    }
}
