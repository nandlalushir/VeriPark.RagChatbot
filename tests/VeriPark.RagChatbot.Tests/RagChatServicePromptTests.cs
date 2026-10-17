using VeriPark.RagChatbot.Api.Chat;
using VeriPark.RagChatbot.Api.Domain;
using Xunit;

namespace VeriPark.RagChatbot.Tests;

public class RagChatServicePromptTests
{
    [Fact]
    public void BuildUserPrompt_ListsNumberedPassagesWithPages()
    {
        var chunks = new[]
        {
            new RetrievedChunk(Guid.NewGuid(), "Guide", 13, 0, "Pricing: $200 per credit pack/month.", 0.8),
            new RetrievedChunk(Guid.NewGuid(), "Guide", 5, 1, "1 credit pack = 25,000 Copilot Credits.", 0.7),
        };

        var prompt = RagChatService.BuildUserPrompt("How much is a pack?", "How much is a pack?", chunks);

        Assert.Contains("[1] (page 13)", prompt);
        Assert.Contains("[2] (page 5)", prompt);
        Assert.Contains("QUESTION: How much is a pack?", prompt);
        Assert.DoesNotContain("standalone question", prompt);
    }

    [Fact]
    public void BuildUserPrompt_WithoutChunks_UsesNoContextMarker()
    {
        var prompt = RagChatService.BuildUserPrompt("Hi", "Hi", []);

        Assert.Contains(PromptTemplates.NoContextMarker, prompt);
    }

    [Fact]
    public void BuildUserPrompt_IncludesRewrittenQuestionWhenItDiffers()
    {
        var prompt = RagChatService.BuildUserPrompt("And the price?", "What is the price of the credit pack?", []);

        Assert.Contains("What is the price of the credit pack?", prompt);
    }
}
