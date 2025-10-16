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

        // Step 1: Collect TxIds related to the party
        var txIds = await _storage.NetSettlementDetails
            .Where(s => s.NetSettlementHeaderId == request.Id &&
                   (s.DebtorBic == party || s.CreditorBic == party))
        .Select(s => s.TxId)
        .Distinct()
        .AsNoTracking()
        .ToListAsync(cancellationToken);

        // Step 2: Retrieve all records for those TxIds
        var details = await _storage.NetSettlementDetails
            .Where(s => s.NetSettlementHeaderId == request.Id && txIds.Contains(s.TxId))
            .OrderByDescending(s => s.Date)
            .ThenBy(s => s.TxId)
            .AsNoTracking()
        .ProjectTo<NetSettlementDetailDto>(_mapper.ConfigurationProvider)
        .ToListAsync(cancellationToken);

        if (details is null || details.Count == 0)
        {
            throw new NotFoundException($"No settlement details found for Id: {request.Id} and Party: {request.Party}");
        }

        // Step 3: Reconciliation logic
        var switchBic = _nsiConfig.SwitchBIC ?? "";

        var reconciliation = details
            .GroupBy(tx => tx.TxId)
            .Select(group =>
            {
                var hops = group.ToList();

                // Identify real sender (first non-switch debtor)
                var first = hops.FirstOrDefault(h => h.DebtorBic != switchBic);
                var last = hops.LastOrDefault(h => h.CreditorBic != switchBic);

                // Calculate transaction amount (non-fee)
                // Use a single representative non-fee hop around the switch to avoid double counting across legs
                var representativeHop = hops
                    .Where(h => !h.IsFee && ((h.DebtorBic == switchBic) ^ (h.CreditorBic == switchBic)))
                    .FirstOrDefault();
                var totalAmount = representativeHop?.Amount
                    ?? hops.Where(h => !h.IsFee).Select(h => h.Amount).FirstOrDefault();

                // Calculate total fees: only those sent from the real debtor to the switch
                var totalFees = hops
                    .Where(h => h.IsFee &&
                                first != null &&
                                h.DebtorBic == first.DebtorBic &&
                                h.CreditorBic == switchBic)
                    .Sum(h => h.Amount);

                return new ReconciliationRow
                {
                    TxId = group.Key.ToString(),
                    EndToEndId = first?.EndToEndId ?? string.Empty,
                    Date = hops.Max(h => h.Date),
                    Amount = totalAmount,
                    SenderBic = first?.DebtorBic ?? "UNKNOWN",
                    ReceiverBic = last?.CreditorBic ?? "UNKNOWN",
                    TotalFees = totalFees
                };
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