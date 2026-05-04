namespace SPSS.App.Features.NSI.Queries;

using SPSS.App.Services;
using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = DownloadNSI)]
public sealed record DownloadSettlementFileQuery(long Id) : IRequest<Response<(MemoryStream, string)>>;

public sealed class DownloadSettlementFileQueryValidator : AbstractValidator<DownloadSettlementFileQuery>
{
    public DownloadSettlementFileQueryValidator()
    {
        RuleFor(e => e.Id).NotNull();
    }
}

public sealed class DownloadSettlementFileQueryHandler(IStorageBroker storage) : IRequestHandler<DownloadSettlementFileQuery, Response<(MemoryStream, string)>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<(MemoryStream, string)>> Handle(DownloadSettlementFileQuery request, CancellationToken cancellationToken)
    {
        var settlementFile = await _storage.NetSettlementHeaders
            .Where(s => s.Id == request.Id)
            .Select(x => new { x.Id, x.OriginalName, x.FilePath })
            .FirstOrDefaultAsync(cancellationToken);
        if (settlementFile is null)
        {
            return Response<(MemoryStream, string)>.NotFound($"Settlement file with ID {request.Id} not found.");
        }

        if (string.IsNullOrWhiteSpace(settlementFile.FilePath))
        {
            return Response<(MemoryStream, string)>.NotFound($"File path for settlement file with ID {request.Id} is empty.");
        }

        var file = FileService.GetFile(settlementFile.FilePath);
        if (file is null)
        {
            return Response<(MemoryStream, string)>.NotFound($"File for settlement file with ID {request.Id} not found.");
        }
        var fileName = string.IsNullOrWhiteSpace(settlementFile.OriginalName) ? "settlement_file.zip" : settlementFile.OriginalName;
        return Response<(MemoryStream, string)>.Success((file, fileName), 0);
    }
}
