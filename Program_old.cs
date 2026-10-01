// using OpenAI;
// using OpenAI.Chat;
// using System.ClientModel;
// using System.Text.Json;
// string apiKey = Environment.GetEnvironmentVariable("HF_API_KEY")
//     ?? throw new InvalidOperationException(
//         "HF_API_KEY environment variable is not set.");

// var client = new ChatClient(
//     model: "deepseek-ai/DeepSeek-R1:fastest",
//     credential: new ApiKeyCredential(apiKey),
//     options: new OpenAIClientOptions
//     {
//         Endpoint = new Uri("https://router.huggingface.co/v1")
//     });

// // ----------------------------------------
// // Conversation history
// // ----------------------------------------

// List<ChatMessage> messages =
// [
//     new SystemChatMessage(
//           """
//     You extract developer information from user input.

//     Return ONLY valid JSON.

//     The JSON must have exactly these properties:

//     name
//     experienceYears
//     skills

//     Example:

//     {
//       "name": "John",
//       "experienceYears": 10,
//       "skills": ["C#", ".NET", "WPF"]
//     }

//     If information is not available, use:
//     - empty string for name
//     - 0 for experienceYears
//     - empty array for skills
//     """
//     )
// ];

// Console.WriteLine("=================================");
// Console.WriteLine("      Hugging Face AI Chat");
// Console.WriteLine("=================================");
// Console.WriteLine("Type 'exit' to quit.");
// Console.WriteLine("Type 'clear' to start a new conversation.");
// Console.WriteLine();

// while (true)
// {
//     Console.Write("You: ");

//     string? question = Console.ReadLine();

//     if (string.IsNullOrWhiteSpace(question))
//         continue;

//     // Exit
//     if (question.Equals("exit", StringComparison.OrdinalIgnoreCase))
//         break;

//     // Clear conversation
//     if (question.Equals("clear", StringComparison.OrdinalIgnoreCase))
//     {
//         messages.Clear();

//         messages.Add(
//             new SystemChatMessage(
//                 "You are a helpful AI assistant. " +
//                 "You are especially good at explaining .NET and C# programming." +
//                 "Explain concepts clearly and provide practical examples."
//             )
//         );

//         Console.WriteLine("Conversation cleared.");
//         Console.WriteLine();

//         continue;
//     }

//     // Add user's message
//     messages.Add(
//         new UserChatMessage(question)
//     );

//     try
//     {
//        string fullResponse = "";

//         await foreach (var update
//             in client.CompleteChatStreamingAsync(messages))
//         {
//             foreach (var contentPart in update.ContentUpdate)
//             {
//                 Console.Write(contentPart.Text);

//                 fullResponse += contentPart.Text;
//             }
//         }

//         Console.WriteLine();
//         Console.WriteLine();

//         // Add AI response to conversation
//         messages.Add(
//             new AssistantChatMessage(fullResponse)
//         );
//     }
//     catch (Exception ex)
//     {
//         Console.WriteLine();
//         Console.WriteLine($"Error: {ex.Message}");
//         Console.WriteLine();
//     }
// }