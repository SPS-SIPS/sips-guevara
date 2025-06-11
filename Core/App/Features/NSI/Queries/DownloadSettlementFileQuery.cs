namespace SPSS.App.Features.NSI.Queries;

using SPSS.App.Services;
using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = DownloadNSI)]
public sealed record DownloadSettlementFileQuery(long Id) : IRequest<Response<MemoryStream>>;

public sealed class DownloadSettlementFileQueryValidator : AbstractValidator<DownloadSettlementFileQuery>
{
    public DownloadSettlementFileQueryValidator()
    {
        RuleFor(e => e.Id).NotNull();
    }
}

public sealed class DownloadSettlementFileQueryHandler(IStorageBroker storage) : IRequestHandler<DownloadSettlementFileQuery, Response<MemoryStream>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<MemoryStream>> Handle(DownloadSettlementFileQuery request, CancellationToken cancellationToken)
    {
        var settlementFile = await _storage.NetSettlementHeaders
            .Where(s => s.Id == request.Id)
            .Select(x => new { x.Id, x.OriginalName, x.FilePath })
            .FirstOrDefaultAsync(cancellationToken);
        if (settlementFile is null)
        {
            return Response<MemoryStream>.NotFound($"Settlement file with ID {request.Id} not found.");
        }

        if (string.IsNullOrWhiteSpace(settlementFile.FilePath))
        {
            return Response<MemoryStream>.NotFound($"File path for settlement file with ID {request.Id} is empty.");
        }

        var file = FileService.GetFile(settlementFile.FilePath);
        if (file is null)
        {
            return Response<MemoryStream>.NotFound($"File for settlement file with ID {request.Id} not found.");
        }

        return Response<MemoryStream>.Success(file, 0);
    }
}
