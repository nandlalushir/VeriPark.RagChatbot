namespace VeriPark.RagChatbot.Api.Domain;

/// <summary>Clean text extracted from a single PDF page (page numbers start at 1).</summary>
public sealed record PageText(int PageNumber, string Text);
