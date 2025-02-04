using System.Xml.Linq;
namespace SPSS.Infrastructure.Services;
public static class ConvertNSIToCSV
{
    public static void Detail(string inputFile, string sheetDir)
    {
        // Combine input file name without extension, type, and .csv
        string inputFileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputFile);
        var outputFile = $"{inputFileNameWithoutExtension}.csv";
        // Load the XML data
        XDocument xmlDoc = XDocument.Load(inputFile);
        // Define the namespace
        XNamespace ns = "urn:iso:std:iso:20022:tech:xsd:pacs.009.001.09";

        // Extract GrpHdr data
        var grpHdr = xmlDoc.Descendants(ns + "GrpHdr").FirstOrDefault();
        string? msgId = grpHdr?.Element(ns + "MsgId")?.Value;
        string? creationDateTime = grpHdr?.Element(ns + "CreDtTm")?.Value;
        string? numberOfTransactions = grpHdr?.Element(ns + "NbOfTxs")?.Value;
        string? totalSettlementAmount = grpHdr?.Element(ns + "TtlIntrBkSttlmAmt")?.Value;
        string? currency = grpHdr?.Element(ns + "TtlIntrBkSttlmAmt")?.Attribute("Ccy")?.Value;
        string? settlementDate = grpHdr?.Element(ns + "IntrBkSttlmDt")?.Value;
        string? instructingAgent = grpHdr?.Element(ns + "InstgAgt")?.Element(ns + "FinInstnId")?.Element(ns + "BICFI")?.Value;
        string? instructedAgent = grpHdr?.Element(ns + "InstdAgt")?.Element(ns + "FinInstnId")?.Element(ns + "BICFI")?.Value;

        // Extract transaction data
        var transactions = xmlDoc.Descendants(ns + "CdtTrfTxInf")
            .Select(tx => new
            {
                EndToEndId = tx.Element(ns + "PmtId")?.Element(ns + "EndToEndId")?.Value,
                TxId = tx.Element(ns + "PmtId")?.Element(ns + "TxId")?.Value,
                InstructionPriority = tx.Element(ns + "PmtTpInf")?.Element(ns + "InstrPrty")?.Value,
                ServiceLevel = tx.Element(ns + "PmtTpInf")?.Element(ns + "SvcLvl")?.Element(ns + "Prtry")?.Value,
                LocalInstrument = tx.Element(ns + "PmtTpInf")?.Element(ns + "LclInstrm")?.Element(ns + "Prtry")?.Value,
                SettlementAmount = tx.Element(ns + "IntrBkSttlmAmt")?.Value,
                Currency = tx.Element(ns + "IntrBkSttlmAmt")?.Attribute("Ccy")?.Value,
                DebtorBIC = tx.Element(ns + "Dbtr")?.Element(ns + "FinInstnId")?.Element(ns + "BICFI")?.Value,
                CreditorBIC = tx.Element(ns + "Cdtr")?.Element(ns + "FinInstnId")?.Element(ns + "BICFI")?.Value
            })
            .OrderBy(tx => tx.TxId)
            .ToList();

        // Create the CSV file
        var csvFilePath = Path.Combine(sheetDir, outputFile);
        using (StreamWriter writer = new(csvFilePath))
        {
            writer.WriteLine("EndToEndId,TxId,InstructionPriority,ServiceLevel,LocalInstrument,SettlementAmount,TransactionCurrency,DebtorBIC,CreditorBIC");
            // Write data for each transaction
            foreach (var tx in transactions)
            {
                writer.WriteLine($"{tx.EndToEndId},{tx.TxId},{tx.InstructionPriority},{tx.ServiceLevel},{tx.LocalInstrument},{tx.SettlementAmount},{tx.Currency},{tx.DebtorBIC},{tx.CreditorBIC}");
            }
        }

        Console.WriteLine($"CSV file created at {csvFilePath}");
    }
    public static void Summary(string inputFile, string sheetDir)
    {
        // Combine input file name without extension, type, and .csv
        string inputFileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputFile);
        var outputFile = $"{inputFileNameWithoutExtension}.summary.csv";

        // Load the XML data
        XDocument xmlDoc = XDocument.Load(inputFile);
        XNamespace ns = "urn:iso:std:iso:20022:tech:xsd:pacs.009.001.09";

        // Extract transaction data
        var transactions = xmlDoc.Descendants(ns + "CdtTrfTxInf")
            .Select(tx => new
            {
                SettlementAmount = decimal.Parse(tx.Element(ns + "IntrBkSttlmAmt")?.Value ?? "0"),
                Currency = tx.Element(ns + "IntrBkSttlmAmt")?.Attribute("Ccy")?.Value,
                DebtorBIC = tx.Element(ns + "Dbtr")?.Element(ns + "FinInstnId")?.Element(ns + "BICFI")?.Value,
                CreditorBIC = tx.Element(ns + "Cdtr")?.Element(ns + "FinInstnId")?.Element(ns + "BICFI")?.Value
            })
            .OrderBy(tx => tx.DebtorBIC)
            .ToList();

        // Prepare CSV file path
        var csvFilePath = Path.Combine(sheetDir, outputFile);

        using StreamWriter writer = new(csvFilePath);
        writer.WriteLine("Acc.,Debit,Credit,Balance");

        // Combine Debtors and Creditors into one list and calculate Debit, Credit, and Balance
        var accounts = transactions
            .SelectMany(tx => new[]
            {
                new { Acc = tx.DebtorBIC, Debit = tx.SettlementAmount, Credit = 0m },
                new { Acc = tx.CreditorBIC, Debit = 0m, Credit = tx.SettlementAmount }
            })
            .GroupBy(acc => acc.Acc)
            .Select(g => new
            {
                Acc = g.Key,
                Debit = g.Sum(x => x.Debit),
                Credit = g.Sum(x => x.Credit),
                Balance = g.Sum(x => x.Credit - x.Debit)
            })
            .ToList();

        // Write each account's data to the CSV file
        foreach (var account in accounts)
        {
            writer.WriteLine($"{account.Acc},{account.Debit},{account.Credit},{account.Balance}");
        }

        // Calculate and write the Net Position
        var netPosition = accounts.Sum(a => a.Balance);
        writer.WriteLine($",,,Net Position,{netPosition}");

        Console.WriteLine($"CSV file created at {csvFilePath}");
    }
}