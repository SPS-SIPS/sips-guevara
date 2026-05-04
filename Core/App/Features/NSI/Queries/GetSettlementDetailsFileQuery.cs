namespace SPSS.App.Features.NSI.Queries;

[Authorize(Roles = QueryNSI)]
[Authorize(Roles = QueryParticipantNSI)]
public sealed record GetSettlementDetailsFileQuery(long Id, string? Party) : IRequest<Response<(MemoryStream, string)>>;
public sealed class GetSettlementDetailsFileQueryValidator : AbstractValidator<GetSettlementDetailsFileQuery>
{
    public GetSettlementDetailsFileQueryValidator(IIdentityService identityService)
    {
        RuleFor(e => e.Id).NotNull();
        RuleFor(e => e.Party)
            .Must(p => !string.IsNullOrWhiteSpace(p))
            .When(e => !identityService.IsInRole(QueryParticipantNSI))
            .WithMessage("Party is required when querying settlement details file");
    }
}
public sealed class GetSettlementDetailsFileQueryHandler(IStorageBroker storage, IMapper mapper, IIdentityService identity, Domain.Models.DTOs.NSI nsiConfig) : IRequestHandler<GetSettlementDetailsFileQuery, Response<(MemoryStream, string)>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;
    private readonly IIdentityService _identity = identity;
    private readonly Domain.Models.DTOs.NSI _nsiConfig = nsiConfig;

    public async ValueTask<Response<(MemoryStream, string)>> Handle(GetSettlementDetailsFileQuery request, CancellationToken cancellationToken)
    {
        var party = _identity.IsInRole(QueryParticipantNSI)
            ? _identity.TenantId
            : request.Party;

        if (string.IsNullOrWhiteSpace(party))
        {
            throw new BadRequestException("Party is required for this query. Contact SPS Team for assistance.");
        }

        // Step 1: Collect TxIds where party is involved in any hop
        var txIds = await _storage.NetSettlementDetails
            .Where(s => s.NetSettlementHeaderId == request.Id &&
                   (s.DebtorBic == party || s.CreditorBic == party))
        .Select(s => s.TxId)
        .Distinct()
        .AsNoTracking()
        .ToListAsync(cancellationToken);

        if (txIds is null || txIds.Count == 0)
        {
            throw new NotFoundException($"No transactions found for Settlement Id: {request.Id} where Party '{party}' is involved as sender or receiver");
        }

        // Step 2: Retrieve all hops for those TxIds to analyze full transaction flow
        var details = await _storage.NetSettlementDetails
            .Where(s => s.NetSettlementHeaderId == request.Id && txIds.Contains(s.TxId))
            .OrderByDescending(s => s.Date)
            .ThenBy(s => s.TxId)
            .ThenBy(s => s.Id)
            .AsNoTracking()
        .ProjectTo<NetSettlementDetailDto>(_mapper.ConfigurationProvider)
        .ToListAsync(cancellationToken);

        if (details is null || details.Count == 0)
        {
            throw new NotFoundException($"No settlement details found for Id: {request.Id} and Party: {party}");
        }

        // Step 3: Reconciliation logic - identify real sender and receiver
        var switchBic = _nsiConfig.CoreBankBIC ?? "";

        var reconciliation = details
            .GroupBy(tx => tx.TxId)
            .Select(group =>
            {
                var hops = group.ToList();

                // Identify real sender (first non-switch debtor)
                var first = hops.FirstOrDefault(h => !h.IsFee && h.DebtorBic != switchBic);
                var last = hops.LastOrDefault(h => !h.IsFee && h.CreditorBic != switchBic);

                // Calculate transaction amount (non-fee)
                // Use a single representative non-fee hop around the switch to avoid double counting across legs
                var representativeHop = hops
                    .Where(h => !h.IsFee && ((h.DebtorBic == switchBic) ^ (h.CreditorBic == switchBic)))
                    .FirstOrDefault();
                var totalAmount = representativeHop?.Amount
                    ?? hops.Where(h => !h.IsFee).Select(h => h.Amount).FirstOrDefault();

                return new
                {
                    TxId = group.Key.ToString(),
                    EndToEndId = first?.EndToEndId ?? string.Empty,
                    Date = hops.Max(h => h.Date),
                    Amount = totalAmount,
                    SenderBic = first?.DebtorBic ?? "UNKNOWN",
                    ReceiverBic = last?.CreditorBic ?? "UNKNOWN",
                    Hops = hops
                };
            })
            // Filter to only include transactions where Party is the real sender or receiver
            .Where(r => r.SenderBic == party || r.ReceiverBic == party)
            .Select(r => new ReconciliationRow
            {
                TxId = r.TxId,
                EndToEndId = r.EndToEndId,
                Date = r.Date,
                Amount = r.Amount,
                SenderBic = r.SenderBic,
                ReceiverBic = r.ReceiverBic,
                // Calculate fees received by the Party from the switch
                TotalFees = r.Hops
                    .Where(h => h.IsFee && h.DebtorBic == switchBic && h.CreditorBic == party)
                    .Sum(h => h.Amount)
            })
            .ToList();

        // Step 4: Write reconciliation output
        var memoryStream = await CSVHelper.WriteRecordsToCsv(reconciliation);

        // Step 5: Return the reconciliation CSV
        var filename = $"Reconciliation_{request.Id}_{party}.csv";
        return Response<(MemoryStream, string)>.Success((memoryStream, filename), 0);
    }
}
file sealed class ReconciliationRow { public string EndToEndId { get; set; } = string.Empty; public string TxId { get; set; } = string.Empty; public DateTimeOffset Date { get; set; } public decimal Amount { get; set; } public string SenderBic { get; set; } = string.Empty; public string ReceiverBic { get; set; } = string.Empty; public decimal TotalFees { get; set; } }