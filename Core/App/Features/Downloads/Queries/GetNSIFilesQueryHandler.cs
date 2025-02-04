namespace SPSS.App.Features.Downloads.Queries;

using static SPSS.Domain.Common.KnownClaims;
// [Authorize(Roles = QueryDownloads)]
public sealed record GetNSIFilesQuery() : IRequest<Response<List<NSISheets>>>;
public sealed class GetNSIFilesQueryHandler(NSI nsiConfig) : IRequestHandler<GetNSIFilesQuery, Response<List<NSISheets>>>
{
    private readonly NSI _nsiConfig = nsiConfig;
    public async ValueTask<Response<List<NSISheets>>> Handle(GetNSIFilesQuery request, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_nsiConfig.ODirectory))
        {
            return Response<List<NSISheets>>.Success([], 0);
        }


        var files = Directory.GetFiles(_nsiConfig.ODirectory, "*.csv")
                     .OrderBy(File.GetCreationTime)
                     .Take(30)
                     .ToArray();

        if (files.Length == 0)
        {
            return Response<List<NSISheets>>.Success([], 0);
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

        return Response<List<NSISheets>>.Success(sheets, 0);
    }
}
