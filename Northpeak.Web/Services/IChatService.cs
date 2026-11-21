using Northpeak.Web.Models;

namespace Northpeak.Web.Services;

public interface IChatService
{
    Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
