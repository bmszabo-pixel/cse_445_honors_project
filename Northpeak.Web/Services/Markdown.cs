using Markdig;
using Microsoft.AspNetCore.Components;

namespace Northpeak.Web.Services;

public static class Markdown
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .UseEmphasisExtras()
        .DisableHtml()
        .Build();

    public static MarkupString ToHtml(string markdown) => new(Markdig.Markdown.ToHtml(markdown, Pipeline));
}
