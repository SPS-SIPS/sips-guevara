namespace SPSS.App.Features.NSI.Queries;

using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = QueryNSI)]
[Authorize(Roles = QueryParticipantNSI)]
public sealed record GetSettlementInfoQuery(string Date) : IRequest<Response<List<NetSettlementHeaderSimpleDto>>>;
public sealed class GetSettlementInfoQueryValidator : AbstractValidator<GetSettlementInfoQuery>
{
    public GetSettlementInfoQueryValidator()
    {
        RuleFor(e => e.Date).NotNull().NotEmpty()
            .WithMessage("Date is required.")
            .Must(BeAValidDate)
            .WithMessage("Invalid date format. Please use a recognized date format (e.g., MM-dd-yyyy, dd-MM-yyyy, yyyy-MM-dd, MM/dd/yyyy, dd/MM/yyyy, yyyy/MM/dd).");
    }

    private bool BeAValidDate(string dateString)
    {
        if (string.IsNullOrEmpty(dateString)) return false;

        if (DateTimeOffset.TryParse(dateString, out _)) return true;

        string[] formats = ["MM-dd-yyyy", "dd-MM-yyyy", "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy", "yyyy/MM/dd"];
        return DateTimeOffset.TryParseExact(dateString, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _);
    }
}
public sealed class GetSettlementInfoQueryHandler(IStorageBroker storage, IMapper mapper) : IRequestHandler<GetSettlementInfoQuery, Response<List<NetSettlementHeaderSimpleDto>>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;

    public async ValueTask<Response<List<NetSettlementHeaderSimpleDto>>> Handle(GetSettlementInfoQuery request, CancellationToken cancellationToken)
    {
        if (!DateTimeOffset.TryParse(request.Date, out DateTimeOffset parsedDate))
        {
            // Add more formats as needed
            string[] formats = ["MM-dd-yyyy", "dd-MM-yyyy", "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy", "yyyy/MM/dd"];
            if (!DateTimeOffset.TryParseExact(request.Date, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsedDate))
            {
                throw new ValidationException("Invalid date format. Please use a recognized date format (e.g., MM-dd-yyyy, dd-MM-yyyy).");
            }
        }

        if (parsedDate > DateTimeOffset.UtcNow)
        {
            throw new ValidationException("Date cannot be in the future.");
        }

        var query = _storage.NetSettlementHeaders.AsNoTracking()
            .Where(x => x.CredDtTm.Date == parsedDate.Date);

        var result = await query.ProjectTo<NetSettlementHeaderSimpleDto>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken);

        return Response<List<NetSettlementHeaderSimpleDto>>.Success(result, 0);
    }
}
