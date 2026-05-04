using System.Text;
using System.Xml.Linq;

namespace SPSS.Infrastructure.Helpers;
public static class NSIHelpers
{
    public static (string feesFile, string transactionsFile) Generate(string inputFile, string outputDir)
    {
        var fileName = Path.GetFileNameWithoutExtension(inputFile);
        var feesOutputName = $"{fileName.Split('_')[1].Split('.')[0]}.fees.xml";
        var transOutputName = $"{fileName.Split('_')[1].Split('.')[0]}.transactions.xml";
        var feesFile = Path.Combine(outputDir, feesOutputName);
        var otherFile = Path.Combine(outputDir, transOutputName);

        // Load the XML file
        XDocument xmlDoc = XDocument.Load(inputFile);

        // Define namespaces
        XNamespace nsPacs = "urn:iso:std:iso:20022:tech:xsd:pacs.009.001.09";

        // Locate all <CdtTrfTxInf> elements
        var transactions = xmlDoc.Descendants(nsPacs + "CdtTrfTxInf").ToList();
        var groupHeader = xmlDoc.Descendants(nsPacs + "GrpHdr").FirstOrDefault();

        // Filter fee transactions
        var fees = transactions.Where(txn => txn.Descendants(nsPacs + "CtgyPurp").FirstOrDefault()?.Value == "FCOL").ToList();

        var other = transactions.Except(fees).ToList();

        // Create a copy of the original document for each output file
        XDocument otherDoc = new(xmlDoc);

        XDocument feesDoc = new(xmlDoc);

        // Remove non-fee transactions from the fees document
        foreach (var txn in other)
        {
            feesDoc.Descendants(nsPacs + "CdtTrfTxInf").Where(x => XNode.DeepEquals(x, txn)).Remove();
        }

        // Remove fee transactions from the other document
        foreach (var txn in fees)
        {
            otherDoc.Descendants(nsPacs + "CdtTrfTxInf").Where(x => XNode.DeepEquals(x, txn)).Remove();
        }

        // Calculate the total amount of fees
        var totalFees = fees.Sum(txn =>
            decimal.Parse(txn.Descendants(nsPacs + "IntrBkSttlmAmt").FirstOrDefault()?.Value ?? "0")
        );
        // get the value of TtlIntrBkSttlmAmt in the group header
        var totalAmount = decimal.Parse(groupHeader?.Descendants(nsPacs + "TtlIntrBkSttlmAmt").FirstOrDefault()?.Value ?? "0");
        var totalOther = totalAmount - totalFees;
        var numberOfTrans = decimal.Parse(groupHeader?.Descendants(nsPacs + "NbOfTxs").FirstOrDefault()?.Value ?? "0");
        var numberOfFees = fees.Count;
        numberOfTrans -= numberOfFees;

        // Update the value of TtlIntrBkSttlmAmt and NbOfTxs in the group header of fees document
        feesDoc.Descendants(nsPacs + "TtlIntrBkSttlmAmt").FirstOrDefault()?.SetValue(totalFees.ToString());
        feesDoc.Descendants(nsPacs + "NbOfTxs").FirstOrDefault()?.SetValue(numberOfFees.ToString());

        foreach (var txn in feesDoc.Descendants(nsPacs + "CdtTrfTxInf"))
        {
            var bicfiElement = txn.Element(nsPacs + "Cdtr")?.Element(nsPacs + "FinInstnId")?.Element(nsPacs + "BICFI");
            if (bicfiElement?.Value == "SPS0SOS0")
            {
                bicfiElement.SetValue("GLXYSOS0");
            }
        }

        // Update the value of TtlIntrBkSttlmAmt and NbOfTxs in the group header of other document
        otherDoc.Descendants(nsPacs + "TtlIntrBkSttlmAmt").FirstOrDefault()?.SetValue(totalOther.ToString());
        otherDoc.Descendants(nsPacs + "NbOfTxs").FirstOrDefault()?.SetValue(numberOfTrans.ToString());
        // update Dbtr in CdtTrfTxInf from ZYABSOSMXXX to 
        // Save the output files
        feesDoc.Save(feesFile);
        otherDoc.Save(otherFile);

        Console.WriteLine("Files generated successfully:");
        Console.WriteLine($"Fees file: {feesFile}");
        Console.WriteLine($"Other file: {otherFile}");

        return (feesFile, otherFile);
    }

    public static string GenerateSummary(string inputFile, string outputDir)
    {
        var fileName = Path.GetFileNameWithoutExtension(inputFile);
        var outputName = $"{fileName.Split('_')[1].Split('.')[0]}.summary.xml";
        var outputFile = Path.Combine(outputDir, outputName);

        // Load the XML file
        XDocument xmlDoc = XDocument.Load(inputFile);

        // Define namespaces
        XNamespace nsPacs = "urn:iso:std:iso:20022:tech:xsd:pacs.009.001.09";

        // Locate all <CdtTrfTxInf> elements
        var transactions = xmlDoc.Descendants(nsPacs + "CdtTrfTxInf").ToList();
        // Summarize the transactions by debtor and creditor BIC codes and calculate the net position
        var summary = transactions
            .Select(tx => new
            {
                SettlementAmount = decimal.Parse(tx.Element(nsPacs + "IntrBkSttlmAmt")?.Value ?? "0"),
                Currency = tx.Element(nsPacs + "IntrBkSttlmAmt")?.Attribute("Ccy")?.Value,
                DebtorBIC = tx.Element(nsPacs + "Dbtr")?.Element(nsPacs + "FinInstnId")?.Element(nsPacs + "BICFI")?.Value,
                CreditorBIC = tx.Element(nsPacs + "Cdtr")?.Element(nsPacs + "FinInstnId")?.Element(nsPacs + "BICFI")?.Value
            })
            .OrderBy(tx => tx.DebtorBIC)
            .GroupBy(tx => new { tx.DebtorBIC, tx.CreditorBIC })
            .Select(g => new NSITransaction
            {
                DebtorBIC = g.Key.DebtorBIC,
                CreditorBIC = g.Key.CreditorBIC,
                Amount = g.Key.DebtorBIC == g.Key.CreditorBIC ? 0 : g.Sum(tx => tx.SettlementAmount),
                Currency = g.First().Currency,
                EndToEndId = RandomString(),
                TxId = RandomString(),
                Priority = "HIGH",
                ServiceLevel = "7502",
                LocalInstrument = "RTGSNetSettlement",
            })
            .ToList();

        // Create a copy of the original document
        XDocument summaryDoc = new(xmlDoc);

        // Remove all transactions from the summary document
        summaryDoc.Descendants(nsPacs + "CdtTrfTxInf").Remove();

        // Add the new CdtTrfTxInf elements 
        foreach (var tx in summary)
        {
            summaryDoc.Root?.Add(GenerateCdtTrfTxInfItem(tx));
        }

        // Save the output file
        summaryDoc.Save(outputFile);

        Console.WriteLine("Summary file generated successfully:");
        return outputFile;
    }



    private static XElement GenerateCdtTrfTxInfItem(NSITransaction tx)
    {
        XNamespace nsPacs = "urn:iso:std:iso:20022:tech:xsd:pacs.009.001.09";
        var ttc = "TTC=" + tx.ServiceLevel;
        return new XElement(nsPacs + "CdtTrfTxInf",
            new XElement(nsPacs + "PmtId",
                new XElement(nsPacs + "EndToEndId", tx.EndToEndId),
                new XElement(nsPacs + "TxId", tx.TxId)
            ),
            new XElement(nsPacs + "PmtTpInf",
                new XElement(nsPacs + "InstrPrty", tx.Priority),
                new XElement(nsPacs + "SvcLvl",
                    new XElement(nsPacs + "Prtry", ttc)
                ),
                new XElement(nsPacs + "LclInstrm",
                    new XElement(nsPacs + "Prtry", tx.LocalInstrument)
                )
            ),
            new XElement(nsPacs + "IntrBkSttlmAmt", new XAttribute("Ccy", tx.Currency!), tx.Amount),
            new XElement(nsPacs + "Dbtr",
                new XElement(nsPacs + "FinInstnId",
                    new XElement(nsPacs + "BICFI", tx.DebtorBIC)
                )
            ),
            new XElement(nsPacs + "Cdtr",
                new XElement(nsPacs + "FinInstnId",
                    new XElement(nsPacs + "BICFI", tx.CreditorBIC)
                )
            )
        );
    }

    private static string RandomString()
    {
        var random = new Random();
        var result = new StringBuilder();
        for (int i = 0; i < 16; i++)
        {
            result.Append(random.Next(0, 9));
        }
        return result.ToString();
    }
}