using System.IO.Compression;
using System.Xml.Linq;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using SPSS.Domain.Models.BL;
using static SPSS.Domain.Common.RTGSHelpers;
namespace SPSS.Infrastructure.Services;

public static class NSIService
{
    public static async Task<long?> ImportXmlToDbAsync(string xmlPath, IStorageBroker db, CancellationToken ct = default)
    {
        var doc = XDocument.Load(xmlPath);
        string fileName = Path.GetFileName(xmlPath);
        var fiCdtTrf = GetFICdtTrfElement(doc);
        if (fiCdtTrf == null) return null;

        var appHdr = doc.Descendants(nsHead + "AppHdr").FirstOrDefault();

        string from = appHdr?
            .Descendants(nsHead + "Fr")
            .Descendants(nsHead + "FinInstnId")
            .Descendants(nsHead + "BICFI")
            .FirstOrDefault()?.Value ?? string.Empty;

        string to = appHdr?
            .Descendants(nsHead + "To")
            .Descendants(nsHead + "FinInstnId")
            .Descendants(nsHead + "BICFI")
            .FirstOrDefault()?.Value ?? string.Empty;

        string bizMsgId = appHdr?
            .Descendants(nsHead + "BizMsgIdr")
            .FirstOrDefault()?.Value ?? string.Empty;

        string msgDefIdr = appHdr?
            .Descendants(nsHead + "MsgDefIdr")
            .FirstOrDefault()?.Value ?? string.Empty;

        string bizSvc = appHdr?
            .Descendants(nsHead + "BizSvc")
            .FirstOrDefault()?.Value ?? string.Empty;

        string originalUserReference = doc
            .Descendants(nsDataPdu + "SenderReference")
            .FirstOrDefault()?.Value ?? string.Empty;


        // Create the NetSettlementHeader
        var netSettlementHeader = new NetSettlementHeader
        {
            MsgId = fiCdtTrf.Descendants(nsPacs009 + "MsgId").FirstOrDefault()?.Value ?? string.Empty,
            OriginalName = fileName,
            CredDtTm = DateTimeOffset.Parse(fiCdtTrf.Descendants(nsPacs009 + "CreDtTm").FirstOrDefault()?.Value ?? DateTimeOffset.UtcNow.ToString()).ToUniversalTime(),
            NumberOfTrans = int.Parse(fiCdtTrf.Descendants(nsPacs009 + "NbOfTxs").FirstOrDefault()?.Value ?? "0"),
            TotalAmount = decimal.Parse(fiCdtTrf.Descendants(nsPacs009 + "TtlIntrBkSttlmAmt").FirstOrDefault()?.Value ?? "0"),
            InterBankSettlementDate = DateTimeOffset.Parse(fiCdtTrf.Descendants(nsPacs009 + "IntrBkSttlmDt").FirstOrDefault()?.Value ?? DateTimeOffset.UtcNow.ToString()).ToUniversalTime(),
            OriginalFile = doc.ToString(),
            From = from,
            To = to,
            BizMsgId = bizMsgId,
            OriginalUserReference = originalUserReference,
        };

        var transactions = fiCdtTrf.Elements(nsPacs009 + "CdtTrfTxInf")
            .Select(tx => new NetSettlementDetail
            {
                EndToEndId = tx.Descendants(nsPacs009 + "EndToEndId").FirstOrDefault()?.Value,
                TxId = tx.Descendants(nsPacs009 + "TxId").FirstOrDefault()?.Value ?? string.Empty,
                UETR = tx.Descendants(nsPacs009 + "UETR").FirstOrDefault()?.Value ?? string.Empty,
                DebtorBic = tx.Descendants(nsPacs009 + "Dbtr").Descendants(nsPacs009 + "BICFI").FirstOrDefault()?.Value,
                CreditorBic = tx.Descendants(nsPacs009 + "Cdtr").Descendants(nsPacs009 + "BICFI").FirstOrDefault()?.Value,
                InstructionPriority = tx.Descendants(nsPacs009 + "InstrPrty").FirstOrDefault()?.Value ?? "HIGH",
                SvcLvl = tx.Descendants(nsPacs009 + "SvcLvl").Descendants(nsPacs009 + "Prtry").FirstOrDefault()?.Value ?? string.Empty,
                LocalInstrument = tx.Descendants(nsPacs009 + "LclInstrm").Descendants(nsPacs009 + "Prtry").FirstOrDefault()?.Value ?? "RTGSNetSettlement",
                Amount = decimal.Parse(tx.Element(nsPacs009 + "IntrBkSttlmAmt")?.Value ?? "0"),
                Date = DateTimeOffset.Parse(tx.Element(nsPacs009 + "IntrBkSttlmDt")?.Value ?? DateTimeOffset.UtcNow.ToString()).ToUniversalTime(),
                CategoryPurpose = tx.Descendants(nsPacs009 + "CtgyPurp").Descendants(nsPacs009 + "Cd").FirstOrDefault()?.Value ?? string.Empty,
                IsFee = string.Equals(
                    tx.Descendants(nsPacs009 + "CtgyPurp").Descendants(nsPacs009 + "Cd").FirstOrDefault()?.Value,
                    "FCOL", StringComparison.OrdinalIgnoreCase)
            }).ToList();

        // Add transactions to the header
        netSettlementHeader.NetSettlementDetails = transactions;

        // Update totals
        netSettlementHeader.TotalFees = transactions.Where(t => t.IsFee).Sum(t => t.Amount);
        netSettlementHeader.TotalTransactions = transactions.Where(t => !t.IsFee).Sum(t => t.Amount);

        await db.NetSettlementHeaders.AddAsync(netSettlementHeader, ct);
        await db.SaveChangesAsync(ct);

        return netSettlementHeader.Id;
    }
    public static async Task<bool> GenerateRTGSFiles(IStorageBroker db, long Id, string outputDirectory, string feeTTC, string txnTTC, ILogger<NSIMonitoring> logger, CancellationToken ct)
    {
        var header = await db.NetSettlementHeaders.Include(h => h.NetSettlementDetails)
        .Where(h => h.Id == Id && !h.Processed && h.Count < 3)
        .SingleOrDefaultAsync(cancellationToken: ct) ?? throw new ArgumentException($"No NetSettlementHeader found with Id {Id}");

        try
        {
            if (header.NetSettlementDetails.Count == 0)
            {
                throw new InvalidOperationException("No NetSettlementDetails found for the specified NetSettlementHeader.");
            }
            // Separate fees and normal transactions
            var feeDetails = header.NetSettlementDetails.Where(d => d.IsFee).ToList();
            var normalDetails = header.NetSettlementDetails.Where(d => !d.IsFee).ToList();

            var nettedFees = NetTransactions(feeDetails, "TTC=" + feeTTC, header.To);
            var nettedNormal = NetTransactions(normalDetails, "TTC=" + txnTTC, header.To);

            // Build XML
            var feesXml = BuildRTGSXML(header, nettedFees, isFee: true);
            var normalXml = BuildRTGSXML(header, nettedNormal, isFee: false);

            // build summary CSV
            var feesSummary = await CSVHelper.WriteRecordsToCsv(nettedFees.Select(x => new
            {
                Dr = x.DebtorBic,
                Cr = x.CreditorBic,
                x.Amount
            }));

            var normalSummary = await CSVHelper.WriteRecordsToCsv(nettedNormal.Select(x => new
            {
                Dr = x.DebtorBic,
                Cr = x.CreditorBic,
                x.Amount
            }));

            // Save XML
            var outputFilePath = await SaveFiles(header, outputDirectory, feesXml, normalXml, feesSummary, normalSummary, ct);

            header.Processed = true;
            header.ProcessedOn = DateTimeOffset.UtcNow.ToUniversalTime();
            header.Count += header.Count < 3 ? 1 : 0;
            header.FilePath = outputFilePath;
            db.NetSettlementHeaders.Update(header);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Generated RTGS Files: {outputFileName}, for NetSettlementHeader Id: {Id}", outputFilePath, Id);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating RTGS files for NetSettlementHeader Id: {Id}", Id);
            header.Processed = false;
            header.Error = "Process Error: " + ex.Message;
            header.Count += header.Count < 3 ? 1 : 0;
            header.ProcessedOn = DateTimeOffset.UtcNow.ToUniversalTime();
            db.NetSettlementHeaders.Update(header);
            await db.SaveChangesAsync(ct);
            return false;
        }
    }

    private static async Task<string> SaveFiles(NetSettlementHeader header, string outputDirectory, XDocument feesXml, XDocument normalXml, MemoryStream feesSummary, MemoryStream normalSummary, CancellationToken ct)
    {
        string feesFileName = header.OriginalName.Replace(".rtgs.xml", "_F.rtgs.xml");
        string normalFileName = header.OriginalName.Replace(".rtgs.xml", "_T.rtgs.xml");


        string feesFilePath = Path.Combine(outputDirectory, feesFileName);
        string normalFilePath = Path.Combine(outputDirectory, normalFileName);

        string feesSummaryPath = Path.Combine(outputDirectory, feesFileName.Replace(".rtgs.xml", ".summary.csv"));
        string normalSummaryPath = Path.Combine(outputDirectory, normalFileName.Replace(".rtgs.xml", ".summary.csv"));

        using (var feesStream = new FileStream(feesFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            await feesXml.SaveAsync(feesStream, SaveOptions.None, ct);

        using (var normalStream = new FileStream(normalFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            await normalXml.SaveAsync(normalStream, SaveOptions.None, ct);

        // Save summaries
        using (var feesSummaryStream = new FileStream(feesSummaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            feesSummary.Seek(0, SeekOrigin.Begin);
            await feesSummary.CopyToAsync(feesSummaryStream, ct);
        }

        using (var normalSummaryStream = new FileStream(normalSummaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            normalSummary.Seek(0, SeekOrigin.Begin);
            await normalSummary.CopyToAsync(normalSummaryStream, ct);
        }

        // Compress both files into a zip
        string zipFileName = header.OriginalName.Replace(".rtgs.xml", ".rtgs.zip");
        string zipFilePath = Path.Combine(outputDirectory, zipFileName);

        using (var zipStream = new FileStream(zipFilePath, FileMode.Create))
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
        {
            archive.CreateEntryFromFile(feesFilePath, feesFileName);
            archive.CreateEntryFromFile(normalFilePath, normalFileName);
            archive.CreateEntryFromFile(feesSummaryPath, Path.GetFileName(feesSummaryPath));
            archive.CreateEntryFromFile(normalSummaryPath, Path.GetFileName(normalSummaryPath));
        }

        // Optionally, delete the original XML files after zipping
        File.Delete(feesFilePath);
        File.Delete(normalFilePath);
        File.Delete(feesSummaryPath);
        File.Delete(normalSummaryPath);

        return zipFilePath;
    }
    public static async Task<bool> GenerateStatisticsAsync(IStorageBroker db, long Id, IBicStatisticsService statisticsService, IMapper mapper, CancellationToken ct)
    {
        var header = await db.NetSettlementHeaders.Where(h => h.Id == Id)
        .Select(x => new
        {
            x.Id,
            x.CredDtTm
        }).SingleOrDefaultAsync(cancellationToken: ct) ?? throw new ArgumentException($"No NetSettlementHeader found with Id {Id}");

        var details = await db.NetSettlementDetails
                   .Where(x => x.NetSettlementHeaderId == Id)
                   .ProjectTo<NetSettlementDetailDto>(mapper.ConfigurationProvider)
                   .ToListAsync(ct);

        var allBICs = details.Select(t => t.DebtorBic)
             .Concat(details.Select(t => t.CreditorBic))
             .Distinct()
             .ToList();

        var statisticsList = new List<BicStatisticsDto>();

        foreach (var bic in allBICs)
        {
            if (string.IsNullOrEmpty(bic)) continue;
            var stats = await statisticsService.CalculateBicStatisticsAsync(bic, details);
            statisticsList.Add(stats);
        }

        var result = statisticsList
            .OrderByDescending(s => s.SentCount + s.ReceivedCount)
            .ThenByDescending(s => s.TotalAmountSent + s.TotalAmountReceived)
            .ToList();

        foreach (var stat in result)
        {
            var existingStat = new BicStatistic
            {
                NetSettlementHeaderId = Id,
                Date = header.CredDtTm,
                SentCount = stat.SentCount,
                TotalAmountSent = stat.TotalAmountSent,
                ReceivedCount = stat.ReceivedCount,
                TotalAmountReceived = stat.TotalAmountReceived,
                TotalFeesReceived = stat.TotalFeesReceived,
                TopReceivers = [.. stat.TopReceivers.Select(r => new BicInteraction
                {
                    Bic = r.Bic,
                    TransactionCount = r.TransactionCount,
                    TotalAmount = r.TotalAmount
                })],
                TopSenders = [.. stat.TopSenders.Select(s => new BicInteraction
                {
                    Bic = s.Bic,
                    TransactionCount = s.TransactionCount,
                    TotalAmount = s.TotalAmount
                })],
                Bic = stat.Bic,
            };

            await db.BicStatistics.AddAsync(existingStat, ct);
        }

        await db.SaveChangesAsync(ct);

        return true;
    }
}