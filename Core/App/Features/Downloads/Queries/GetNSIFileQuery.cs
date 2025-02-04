namespace SPSS.App.Features.Downloads.Queries;

using static SPSS.Domain.Common.KnownClaims;
// [Authorize(Roles = QueryDownloads)]
public sealed record GetNSIFileQuery(string FileName) : IRequest<MemoryStream?>;
public sealed class GetNSIFileQueryHandler(NSI nsiConfig) : IRequestHandler<GetNSIFileQuery, MemoryStream?>
{
    private readonly NSI _nsiConfig = nsiConfig;
    public async ValueTask<MemoryStream?> Handle(GetNSIFileQuery request, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_nsiConfig.ODirectory))
        {
            return null;
        }

        var filePath = Path.Combine(_nsiConfig.ODirectory, request.FileName);

        if (!File.Exists(filePath))
        {
            return null;
        }

        var memoryStream = new MemoryStream();
        await using (var stream = new FileStream(filePath, FileMode.Open))
        {
            await stream.CopyToAsync(memoryStream, cancellationToken);
        }

        memoryStream.Position = 0;
        return memoryStream;
    }
}
