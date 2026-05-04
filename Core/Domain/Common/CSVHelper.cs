using CsvHelper;
namespace SPSS.Domain.Common;

public static class CSVHelper
{
    public static async Task<MemoryStream> WriteRecordsToCsv<T>(IEnumerable<T> records)
    {
        var memoryStream = new MemoryStream();
        using (var writer = new StreamWriter(memoryStream, new System.Text.UTF8Encoding(false), 1024, leaveOpen: true))
        using (var csv = new CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture))
        {
            await csv.WriteRecordsAsync(records);
            await writer.FlushAsync();
        }
        memoryStream.Position = 0;
        return memoryStream;
    }
}