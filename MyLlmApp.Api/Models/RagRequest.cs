namespace MyLlmApp.Api.Models;

public class RagRequest
{
    public string ConversationId { get; set; } = "";

    public string Question { get; set; } = "";
}