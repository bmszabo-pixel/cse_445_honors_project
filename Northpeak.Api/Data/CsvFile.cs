using System.Globalization;
using CsvHelper;

namespace Northpeak.Api.Data;

public static class CsvFile
{
    private static readonly CsvReaderOptions Options = new()
    {
        Separator = ',',
        TrimWhitespace = true,
        FormatProvider = CultureInfo.InvariantCulture
    };

    public static List<T> Read<T>(string path)
    {
        using var reader = CsvReader.FromFile(path, Options);
        return reader.GetRecords<T>().ToList();
    }
}
