using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Northpeak.Api.Data;

public sealed class KnowledgeBase(string catalogText, string policiesText)
{
    public string CatalogText { get; } = catalogText;

    public string PoliciesText { get; } = policiesText;

    public static KnowledgeBase Load(string catalogPath, string policiesPath) =>
        new(ReadDocx(catalogPath), ReadDocx(policiesPath));

    private static string ReadDocx(string path)
    {
        using var document = WordprocessingDocument.Open(path, isEditable: false);
        var body = document.MainDocumentPart?.Document?.Body;
        if (body is null)
        {
            return string.Empty;
        }

        var lines = body
            .Descendants<Paragraph>()
            .Select(paragraph => paragraph.InnerText.Trim())
            .Where(text => text.Length > 0);

        return string.Join(Environment.NewLine, lines);
    }
}
