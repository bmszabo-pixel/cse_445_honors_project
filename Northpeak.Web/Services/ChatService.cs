using System.Net.Http.Json;
using Northpeak.Shared;

namespace Northpeak.Web.Services;

public sealed class ChatService(HttpClient httpClient)
{
    public async Task<ChatResponse> SendAsync(ChatRequest request)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync("chat", request);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException("The Northpeak API can't be reached. Make sure Northpeak.Api is running.", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new ApiException("The assistant took too long to respond. Please try again.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var error = await ReadErrorAsync(response);
                throw new ApiException(error ?? $"The Northpeak API returned an error ({(int)response.StatusCode}).");
            }

            return await response.Content.ReadFromJsonAsync<ChatResponse>()
                ?? throw new ApiException("The assistant returned an empty response.");
        }
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            return (await response.Content.ReadFromJsonAsync<ApiError>())?.Error;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }
}
