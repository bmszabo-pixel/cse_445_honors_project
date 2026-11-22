using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Options;
using Northpeak.Api.Catalog;
using Northpeak.Api.Data;
using Northpeak.Shared;

namespace Northpeak.Api.Chat;

public sealed class GeminiShoppingAssistant(
    IOptions<GeminiOptions> options,
    ShoppingTools tools,
    ProductCatalog catalog,
    KnowledgeBase knowledge,
    ILogger<GeminiShoppingAssistant> logger)
{
    private const int MaxToolRounds = 6;
    private const int MaxHistoryTurns = 30;

    private readonly GeminiOptions _options = options.Value;
    private readonly Lazy<Client> _client = new(() => new Client(apiKey: options.Value.ApiKey));

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<ChatResponse> ReplyAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        var session = new ToolSession(request.TripContext ?? TripContext.Empty);
        var contents = BuildContents(request);
        var config = new GenerateContentConfig
        {
            SystemInstruction = new Content
            {
                Parts = [new Part { Text = SystemPrompt.Build(knowledge, catalog, DateOnly.FromDateTime(DateTime.Now)) }]
            },
            Tools = [tools.Declarations],
            Temperature = 0.4f
        };

        string? reply = null;
        for (var round = 0; round < MaxToolRounds; round++)
        {
            var response = await _client.Value.Models.GenerateContentAsync(_options.Model, contents, config, cancellationToken);
            var calls = response.FunctionCalls;
            if (calls is null || calls.Count == 0)
            {
                reply = response.Text ?? string.Empty;
                break;
            }

            if (response.Candidates?.FirstOrDefault()?.Content is { } modelTurn)
            {
                contents.Add(modelTurn);
            }

            var results = new List<Part>();
            foreach (var call in calls)
            {
                logger.LogInformation("Gemini called {Tool}", call.Name);
                var result = await tools.ExecuteAsync(call.Name ?? string.Empty, call.Args, session, cancellationToken);
                results.Add(new Part
                {
                    FunctionResponse = new FunctionResponse { Id = call.Id, Name = call.Name, Response = result }
                });
            }

            contents.Add(new Content { Role = "user", Parts = results });
        }

        reply ??= "I couldn't finish that request. Could you try asking again in a simpler way?";

        return new ChatResponse(
            reply,
            session.Trip,
            session.Activity,
            ProductMentions.Find(reply, catalog),
            session.Forecast,
            session.Orders);
    }

    private List<Content> BuildContents(ChatRequest request)
    {
        var history = (request.History ?? [])
            .Where(turn => !string.IsNullOrWhiteSpace(turn.Content))
            .TakeLast(MaxHistoryTurns)
            .SkipWhile(turn => turn.Role == ChatRoles.Assistant)
            .Select(turn => new Content
            {
                Role = turn.Role == ChatRoles.Assistant ? "model" : "user",
                Parts = [new Part { Text = turn.Content }]
            });

        return [.. history, new Content { Role = "user", Parts = [new Part { Text = request.Message }] }];
    }
}
