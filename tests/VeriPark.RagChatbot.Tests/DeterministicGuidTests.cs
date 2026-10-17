using VeriPark.RagChatbot.Api.Ingestion;
using Xunit;

namespace VeriPark.RagChatbot.Tests;

public class DeterministicGuidTests
{
    [Fact]
    public void SameInput_GivesSameGuid() =>
        Assert.Equal(DeterministicGuid.Create("Guide|1|0"), DeterministicGuid.Create("Guide|1|0"));

    [Fact]
    public void DifferentInput_GivesDifferentGuid() =>
        Assert.NotEqual(DeterministicGuid.Create("Guide|1|0"), DeterministicGuid.Create("Guide|1|1"));
}
