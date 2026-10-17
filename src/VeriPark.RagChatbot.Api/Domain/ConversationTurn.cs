namespace VeriPark.RagChatbot.Api.Domain;

/// <summary>One question and the answer the chatbot gave to it.</summary>
public sealed record ConversationTurn(
    string Question,
    string Answer,
    IReadOnlyList<Citation> Citations,
    DateTimeOffset AskedAtUtc);
