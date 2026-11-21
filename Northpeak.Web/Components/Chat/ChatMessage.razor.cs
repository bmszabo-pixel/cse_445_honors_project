using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Northpeak.Web.Models;

namespace Northpeak.Web.Components.Chat;

public partial class ChatMessage
{
    [Parameter, EditorRequired]
    public Northpeak.Web.Models.ChatMessage Message { get; set; } = default!;

    private string FormattedContent => FormatMarkdown(Message.Content);

    private static string FormatMarkdown(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var encoded = WebUtility.HtmlEncode(text);
        encoded = Regex.Replace(encoded, @"\*\*(.*?)\*\*", "<strong>$1</strong>");
        encoded = Regex.Replace(encoded, @"`([^`]+)`", "<code>$1</code>");

        var lines = encoded.Split('\n');
        var builder = new System.Text.StringBuilder();
        var inList = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.StartsWith("- ") || line.StartsWith("* "))
            {
                if (!inList)
                {
                    builder.Append("<ul>");
                    inList = true;
                }
                builder.Append($"<li>{line.Substring(2)}</li>");
            }
            else
            {
                if (inList)
                {
                    builder.Append("</ul>");
                    inList = false;
                }

                if (!string.IsNullOrEmpty(line))
                {
                    builder.Append($"<p>{line}</p>");
                }
            }
        }

        if (inList)
        {
            builder.Append("</ul>");
        }

        return builder.ToString();
    }
}
