using Microsoft.Extensions.Options;
using VeriPark.RagChatbot.Api.Configuration;
using VeriPark.RagChatbot.Api.Domain;
using VeriPark.RagChatbot.Api.Ingestion;
using Xunit;

namespace VeriPark.RagChatbot.Tests;

public class RecursiveTextChunkerTests
{
    private static RecursiveTextChunker CreateChunker(int size = 200, int overlap = 40) =>
        new(Options.Create(new RagOptions { ChunkSize = size, ChunkOverlap = overlap }));

    private static string Sentences(int count) =>
        string.Join(" ", Enumerable.Range(1, count).Select(i => $"Sentence number {i} is about licensing."));

    [Fact]
    public void ShortPage_StaysInOneChunk()
    {
        var chunker = CreateChunker();

        var chunks = chunker.Chunk("Guide", [new PageText(3, "A short page.")]);

        var chunk = Assert.Single(chunks);
        Assert.Equal(3, chunk.PageNumber);
        Assert.Equal("A short page.", chunk.Text);
    }

    [Fact]
    public void LongPage_IsSplit_AndNoChunkExceedsTheLimit()
    {
        var chunker = CreateChunker(size: 200, overlap: 40);

        var chunks = chunker.Chunk("Guide", [new PageText(1, Sentences(40))]);

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, chunk => Assert.True(chunk.Text.Length <= 200, $"Chunk too long: {chunk.Text.Length}"));
    }

    [Fact]
    public void NeighbouringChunks_ShareOverlappingText()
    {
        var chunker = CreateChunker(size: 200, overlap: 60);

        var chunks = chunker.Chunk("Guide", [new PageText(1, Sentences(40))]);

        for (var i = 1; i < chunks.Count; i++)
        {
            var start = chunks[i].Text[..15];
            Assert.Contains(start, chunks[i - 1].Text);
        }
    }

    [Fact]
    public void Chunks_NeverCrossPages_AndKeepTheirPageNumber()
    {
        var chunker = CreateChunker();

        var chunks = chunker.Chunk("Guide",
        [
            new PageText(1, Sentences(20)),
            new PageText(2, "Second page text."),
        ]);

        Assert.Contains(chunks, c => c.PageNumber == 1);
        Assert.Equal(2, chunks[^1].PageNumber);
        Assert.Equal("Second page text.", chunks[^1].Text);
    }

    [Fact]
    public void ChunkIndexes_AreSequential()
    {
        var chunker = CreateChunker();

        var chunks = chunker.Chunk("Guide", [new PageText(1, Sentences(30)), new PageText(2, Sentences(30))]);

        Assert.Equal(Enumerable.Range(0, chunks.Count), chunks.Select(c => c.ChunkIndex));
    }

    [Fact]
    public void EmptyPages_AreSkipped()
    {
        var chunker = CreateChunker();

        var chunks = chunker.Chunk("Guide", [new PageText(1, "   \n  "), new PageText(2, "Real text.")]);

        Assert.Single(chunks);
        Assert.Equal(2, chunks[0].PageNumber);
    }

    [Fact]
    public void VeryLongWordWithoutSpaces_IsHardSplit()
    {
        var chunker = CreateChunker(size: 200, overlap: 40);

        var chunks = chunker.Chunk("Guide", [new PageText(1, new string('x', 650))]);

        Assert.True(chunks.Count >= 4);
        Assert.All(chunks, c => Assert.True(c.Text.Length <= 200));
    }

    [Fact]
    public void Constructor_RejectsOverlapLargerThanChunkSize()
    {
        var options = Options.Create(new RagOptions { ChunkSize = 200, ChunkOverlap = 200 });

        Assert.Throws<ArgumentException>(() => new RecursiveTextChunker(options));
    }
}
