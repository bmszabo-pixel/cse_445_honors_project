using System.Net.Http.Json;
using Northpeak.Web.Models;

namespace Northpeak.Web.Services;

public class ChatService : IChatService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChatService> _logger;

    public ChatService(HttpClient httpClient, ILogger<ChatService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/chat", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Chat endpoint returned status {StatusCode}: {Body}", response.StatusCode, errorBody);
                return new ChatResponse(
                    SessionId: request.SessionId,
                    Reply: "The chat service encountered an error while processing your request. Please try again.",
                    ToolCalls: null,
                    Success: false,
                    Error: $"Status code {response.StatusCode}"
                );
            }

            var result = await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken: cancellationToken);
            return result ?? new ChatResponse(
                SessionId: request.SessionId,
                Reply: "Empty response received from the assistant.",
                ToolCalls: null,
                Success: false,
                Error: "Empty response"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception communicating with chat API");
            return new ChatResponse(
                SessionId: request.SessionId,
                Reply: "Unable to reach the Northpeak assistant service. Please check your backend connection.",
                ToolCalls: null,
                Success: false,
                Error: ex.Message
            );
        }
    }
}
