using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using VeriPark.RagChatbot.Api.Domain;

namespace VeriPark.RagChatbot.Api.Ingestion;

/// <summary>
/// Extracts text with PdfPig (pure .NET, no native dependencies).
/// Words are grouped into visual lines and paragraphs, and lines that repeat on many pages
/// (page footers such as "Licensing Guide | July 2026 Page | 4") are removed so they do not pollute the chunks.
/// </summary>
public sealed partial class PdfPigTextExtractor : IPdfTextExtractor
{
    private const int MinimumPagesForBoilerplateDetection = 4;
    private const int MaximumBoilerplateLineLength = 150;
    private const double ParagraphGapFactor = 1.8;

    private readonly ILogger<PdfPigTextExtractor> _logger;

    public PdfPigTextExtractor(ILogger<PdfPigTextExtractor> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<PageText> Extract(string pdfPath)
    {
        if (!File.Exists(pdfPath))
        {
            throw new FileNotFoundException($"PDF file not found at '{pdfPath}'. Check the Rag:PdfPath setting.", pdfPath);
        }

        using var document = PdfDocument.Open(pdfPath);

        var pages = new List<(int Number, List<string> Lines)>();
        foreach (var page in document.GetPages())
        {
            pages.Add((page.Number, ExtractLines(page)));
        }

        var repeatedLines = FindRepeatedLines(pages);

        var result = new List<PageText>();
        foreach (var (number, lines) in pages)
        {
            var kept = lines.Where(line => line.Length == 0 || !repeatedLines.Contains(Normalize(line)));
            var text = string.Join('\n', kept).Trim();
            if (text.Length > 0)
            {
                result.Add(new PageText(number, text));
            }
        }

        _logger.LogInformation(
            "Extracted text from {PageCount} pages ({NonEmpty} with text); removed {Repeated} repeated header/footer patterns.",
            pages.Count, result.Count, repeatedLines.Count);

        return result;
    }

    private static List<string> ExtractLines(Page page)
    {
        var words = page.GetWords()
            .Where(word => !string.IsNullOrWhiteSpace(word.Text))
            .OrderByDescending(word => word.BoundingBox.Bottom)
            .ToList();

        var lineBuilders = new List<LineBuilder>();
        foreach (var word in words)
        {
            var current = lineBuilders.Count > 0 ? lineBuilders[^1] : null;
            if (current is not null && current.Accepts(word))
            {
                current.Words.Add(word);
            }
            else
            {
                lineBuilders.Add(new LineBuilder(word));
            }
        }

        var lines = new List<string>();
        LineBuilder? previous = null;
        foreach (var builder in lineBuilders)
        {
            if (previous is not null && previous.Bottom - builder.Bottom > ParagraphGapFactor * builder.Height)
            {
                lines.Add(string.Empty); // blank line = paragraph break
            }

            lines.Add(string.Join(' ', builder.Words.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)));
            previous = builder;
        }

        return lines;
    }

    private static HashSet<string> FindRepeatedLines(List<(int Number, List<string> Lines)> pages)
    {
        var repeated = new HashSet<string>(StringComparer.Ordinal);
        if (pages.Count < MinimumPagesForBoilerplateDetection)
        {
            return repeated;
        }

        var threshold = Math.Max(3, pages.Count / 2);
        var pageCountPerPattern = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var (_, lines) in pages)
        {
            var patternsOnThisPage = lines
                .Where(line => line.Length > 0 && line.Length <= MaximumBoilerplateLineLength)
                .Select(Normalize)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var pattern in patternsOnThisPage)
            {
                pageCountPerPattern[pattern] = pageCountPerPattern.GetValueOrDefault(pattern) + 1;
            }
        }

        foreach (var (pattern, count) in pageCountPerPattern)
        {
            if (count >= threshold)
            {
                repeated.Add(pattern);
            }
        }

        return repeated;
    }

    /// <summary>Digits are replaced so that "Page | 4" and "Page | 5" count as the same pattern.</summary>
    private static string Normalize(string line) =>
        DigitsRegex().Replace(line.Trim(), "#");

    [GeneratedRegex(@"\d+")]
    private static partial Regex DigitsRegex();

    private sealed class LineBuilder
    {
        public LineBuilder(Word first)
        {
            Bottom = first.BoundingBox.Bottom;
            Height = Math.Max(first.BoundingBox.Height, 1);
            Words = [first];
        }

        public double Bottom { get; }

        public double Height { get; }

        public List<Word> Words { get; }

        public bool Accepts(Word word) =>
            Math.Abs(Bottom - word.BoundingBox.Bottom) <= Math.Max(2, Height * 0.5);
    }
}
