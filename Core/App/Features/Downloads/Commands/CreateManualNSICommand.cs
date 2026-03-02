using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SPSS.Domain.Models.BL;
using SPSS.Domain.Models.DTOs;
using SPSS.Domain.Interfaces;
using System.Text.Json;

namespace SPSS.App.Features.Downloads.Commands;

public class ManualNSITransactionDto
{
    public string DebtorBic { get; set; } = string.Empty;
    public string CreditorBic { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsFee { get; set; }
}

[Authorize(Roles = CreateManualNSI)]
public class CreateManualNSICommand : IRequest<Response<string>>
{
    public DateTimeOffset SettlementDate { get; set; }
    public List<ManualNSITransactionDto> Transactions { get; set; } = new();
}

public class CreateManualNSICommandHandler : IRequestHandler<CreateManualNSICommand, Response<string>>
{
    private readonly IStorageBroker _db;
    private readonly INSIGeneratorService _nsiGeneratorService;
    private readonly SPSS.Domain.Models.DTOs.NSI _nsiOptions;

    public CreateManualNSICommandHandler(IStorageBroker db, INSIGeneratorService nsiGeneratorService, SPSS.Domain.Models.DTOs.NSI nsiConfig)
    {
        _db = db;
        _nsiGeneratorService = nsiGeneratorService;
        _nsiOptions = nsiConfig;
    }

    public async ValueTask<Response<string>> Handle(CreateManualNSICommand request, CancellationToken cancellationToken)
    {
        var allBics = request.Transactions.SelectMany(tx => new[] { tx.DebtorBic, tx.CreditorBic }).ToList();
        var duplicateBics = allBics.GroupBy(b => b).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

        if (duplicateBics.Any())
        {
            return new Response<string>("")
            {
                IsSuccess = false,
                Message = $"Validation failed: Banks can only appear once per NSI file. Duplicate banks found: {string.Join(", ", duplicateBics)}"
            };
        }

        var msgId = $"MNSI_{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        var settlementDate = request.SettlementDate.Year < 2000
            ? DateTimeOffset.UtcNow.ToUniversalTime()
            : request.SettlementDate.ToUniversalTime();

        var header = new NetSettlementHeader
        {
            MsgId = msgId,
            OriginalName = $"{msgId}.manual.rtgs.xml",
            CredDtTm = DateTimeOffset.UtcNow.ToUniversalTime(),
            NumberOfTrans = request.Transactions.Count,
            TotalAmount = request.Transactions.Sum(t => t.Amount),
            InterBankSettlementDate = settlementDate,
            OriginalFile = JsonSerializer.Serialize(request.Transactions),
            From = _nsiOptions.ClearingEntityBIC,
            To = _nsiOptions.CoreBankBIC,
            CentralBankBiC = _nsiOptions.CoreBankBIC,
            BizMsgId = $"BIZ_{msgId}",
            OriginalUserReference = $"MANUAL_{msgId}",
            Count = 0,
            Processed = false,
        };

        var details = request.Transactions.Select((tx, i) => new NetSettlementDetail
        {
            EndToEndId = $"E2E_{msgId}_{i}",
            TxId = $"TX_{msgId}_{i}",
            UETR = Guid.NewGuid().ToString(),
            DebtorBic = tx.DebtorBic,
            CreditorBic = tx.CreditorBic,
            InstructionPriority = "HIGH",
            SvcLvl = "",
            LocalInstrument = "RTGSNetSettlement",
            Amount = tx.Amount,
            Date = settlementDate,
            CategoryPurpose = tx.IsFee ? "FCOL" : "OTHR",
            IsFee = tx.IsFee
        }).ToList();

        header.NetSettlementDetails = details;
        header.TotalFees = details.Where(t => t.IsFee).Sum(t => t.Amount);
        header.TotalTransactions = details.Where(t => !t.IsFee).Sum(t => t.Amount);

        await _db.NetSettlementHeaders.AddAsync(header, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        var outputDirectory = _nsiOptions.Directory;
        if (string.IsNullOrEmpty(outputDirectory))
        {
            outputDirectory = "app"; // Safeguard failover
        }

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var success = await _nsiGeneratorService.GenerateRTGSFilesAsync(header.Id, outputDirectory, _nsiOptions.FeeTTC ?? "", _nsiOptions.TxnTTC ?? "", cancellationToken);

        if (success)
        {
            return new Response<string>(header.FilePath ?? "") { Message = "Manual NSI generated successfully." };
        }
        else
        {
            return new Response<string>("") { IsSuccess = false, Message = $"Failed to generate manual NSI. Error: {header.Error}" };
        }
    }
}
