namespace VeriPark.RagChatbot.AnswerCollector;

/// <summary>The seven questions from the assessment, word for word and in the original order.</summary>
internal static class AssessmentQuestions
{
    public static readonly IReadOnlyList<string> All =
    [
        "According to the licensing guide, what is a Copilot Credit, and what factors determine how many Copilot Credits are consumed when an agent responds to a prompt or performs a task?",
        "How many Copilot Credits are included in a single Copilot Studio Copilot Credit pack, and what is its monthly price? Additionally, in what situation does the guide recommend signing up for the pay-as-you-go meter in addition to credit packs?",
        "List the default Dataverse capacities (Database, File, Log) provided per tenant for Copilot Studio. Briefly explain the purpose of each capacity type.",
        "Which licenses or pay-as-you-go meters enable Managed Environments in the context of Copilot Studio, and why is this relevant for enterprise governance?",
        "What is the pricing of the Microsoft Copilot Studio pay-as-you-go Copilot Credit meter, and how is usage measured and billed?",
        "Based on your answer to Question 5, compare the pay-as-you-go meter with the Copilot Credit pack subscription model in terms of flexibility and cost predictability.",
        "What was the first question I asked you in this conversation?",
    ];
}
