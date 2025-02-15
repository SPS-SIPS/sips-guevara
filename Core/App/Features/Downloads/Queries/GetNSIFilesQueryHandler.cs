namespace SPSS.App.Features.Downloads.Queries;

using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = QueryDownloads)]
public sealed record GetNSIFilesQuery(string Type = "csv") : IRequest<Response<List<NSISheets>>>;
public sealed class GetNSIFilesQueryHandler(NSI nsiConfig) : IRequestHandler<GetNSIFilesQuery, Response<List<NSISheets>>>
{
    private readonly NSI _nsiConfig = nsiConfig;
    public ValueTask<Response<List<NSISheets>>> Handle(GetNSIFilesQuery request, CancellationToken cancellationToken)
    {
        var locationToSearch = request.Type == "csv" ? "output" : "backup";
        var output = Path.Combine(_nsiConfig.Directory, locationToSearch);

        if (!Directory.Exists(output))
        {
            return ValueTask.FromResult(Response<List<NSISheets>>.Success([], 0));
        }


        var files = Directory.GetFiles(output, "*.csv")
                     .OrderByDescending(File.GetCreationTime)
                     .Take(30)
                     .ToArray();

        if (files.Length == 0)
        {
            return ValueTask.FromResult(Response<List<NSISheets>>.Success([], 0));
        }

        var sheets = new List<NSISheets>();

        foreach (var file in files)
        {

            var fileName = Path.GetFileName(file);
            var sheet = new NSISheets
            {
                FileName = fileName,
                FileSize = new FileInfo(file).Length,
                CreatedAt = File.GetCreationTime(file)
            };

            sheets.Add(sheet);
        }

        return ValueTask.FromResult(Response<List<NSISheets>>.Success(sheets, 0));
    }
}
