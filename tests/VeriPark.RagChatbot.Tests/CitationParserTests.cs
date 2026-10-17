using VeriPark.RagChatbot.Api.Chat;
using VeriPark.RagChatbot.Api.Domain;
using Xunit;

namespace VeriPark.RagChatbot.Tests;

public class CitationParserTests
{
    private static RetrievedChunk Chunk(int page, double score = 0.5) =>
        new(Guid.NewGuid(), "Guide", page, 0, $"Text of page {page}.", score);

    [Theory]
    [InlineData("A credit is a unit [p.4].", new[] { 4 })]
    [InlineData("See [p.5][p.13] for details.", new[] { 5, 13 })]
    [InlineData("See [p.5, p.13].", new[] { 5, 13 })]
    [InlineData("No citation here.", new int[0])]
    public void ExtractPages_FindsPageReferences(string answer, int[] expected)
    {
        var pages = CitationParser.ExtractPages(answer);

        Assert.Equal(expected.OrderBy(p => p), pages.OrderBy(p => p));
    }

    [Fact]
    public void Build_OnlyReturnsPagesThatWereReallyRetrieved()
    {
        var retrieved = new[] { Chunk(4), Chunk(5) };

        var citations = CitationParser.Build("Fact [p.4] and an invented one [p.99].", retrieved);

        var citation = Assert.Single(citations);
        Assert.Equal(4, citation.PageNumber);
    }

    [Fact]
    public void Build_WithoutPageReferences_ReturnsNoCitations()
    {
        var citations = CitationParser.Build("I couldn't find that in the Copilot Studio Licensing Guide.", [Chunk(4)]);

        Assert.Empty(citations);
    }

    [Fact]
    public void Build_TruncatesLongSnippets()
    {
        var longChunk = new RetrievedChunk(Guid.NewGuid(), "Guide", 2, 0, new string('a', 1000), 0.9);

        var citation = Assert.Single(CitationParser.Build("Fact [p.2]", [longChunk]));

        Assert.True(citation.Snippet.Length < 300);
    }
}
