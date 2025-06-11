using System.Xml.Linq;
using SPSS.Domain.Models.BL;

namespace SPSS.Infrastructure.Helpers;

public static class RTGSHelpers
{
    public static readonly XNamespace nsDataPdu = "urn:swift:saa:xsd:saa.2.0";
    public static readonly XNamespace nsHead = "urn:iso:std:iso:20022:tech:xsd:head.001.001.02";
    public static readonly XNamespace nsPacs009 = "urn:iso:std:iso:20022:tech:xsd:pacs.009.001.09";
    public static XElement? GetFICdtTrfElement(XDocument doc)
    {
        var pacsDocument = doc.Descendants(nsPacs009 + "Document").FirstOrDefault();
        return pacsDocument?.Element(nsPacs009 + "FICdtTrf");
    }

    public static (List<XElement> feeList, List<XElement> normalList) SplitTransactions(XElement fiCdtTrfElement)
    {
        var allTransactions = fiCdtTrfElement.Elements(nsPacs009 + "CdtTrfTxInf").ToList();
        var feeList = new List<XElement>();
        var normalList = new List<XElement>();

        foreach (var tx in allTransactions)
        {
            var ctgyPurp = tx
                .Descendants(nsPacs009 + "CtgyPurp")
                .Descendants(nsPacs009 + "Cd")
                .FirstOrDefault()?.Value;

            bool isFee = string.Equals(ctgyPurp, "FCOL", StringComparison.OrdinalIgnoreCase);
            if (isFee)
                feeList.Add(new XElement(tx));
            else
                normalList.Add(new XElement(tx));
        }
        return (feeList, normalList);
    }

    public static XElement BuildDocument(XElement fiCdtTrfElement, List<XElement> txList)
    {
        return new XElement(nsPacs009 + "Document",
            new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
            new XElement(nsPacs009 + "FICdtTrf",
                new XElement(fiCdtTrfElement.Element(nsPacs009 + "GrpHdr")),
                txList
            )
        );
    }

    public static List<NetSettlementDetail> NetTransactions(IEnumerable<NetSettlementDetail> details, string svcLvlValue, string To)
    {
        var netPositions = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var tx in details)
        {
            if (string.IsNullOrWhiteSpace(tx.DebtorBic) || string.IsNullOrWhiteSpace(tx.CreditorBic))
                continue;

            if (!netPositions.ContainsKey(tx.DebtorBic))
                netPositions[tx.DebtorBic] = 0;
            netPositions[tx.DebtorBic] -= tx.Amount;

            if (!netPositions.ContainsKey(tx.CreditorBic))
                netPositions[tx.CreditorBic] = 0;
            netPositions[tx.CreditorBic] += tx.Amount;
        }

        var nettedTxs = new List<NetSettlementDetail>();

        foreach (var kvp in netPositions.Where(kvp => kvp.Value != 0).OrderBy(kvp => kvp.Value))
        {
            var bic = kvp.Key;
            var net = kvp.Value;

            var detail = new NetSettlementDetail
            {
                EndToEndId = Guid.NewGuid().ToString("N"),
                TxId = Guid.NewGuid().ToString("N"),
                InstructionPriority = "HIGH",
                SvcLvl = svcLvlValue,
                LocalInstrument = "RTGSNetSettlement",
                Amount = Math.Abs(net),
                DebtorBic = net > 0 ? To : bic,
                CreditorBic = net > 0 ? bic : To,
                Date = DateTimeOffset.UtcNow,
            };
            nettedTxs.Add(detail);
        }

        return nettedTxs;
    }
    public static XDocument BuildRTGSXML(NetSettlementHeader header, IEnumerable<NetSettlementDetail> details, bool isFee)
    {
        var nsPacs009 = "urn:iso:std:iso:20022:tech:xsd:pacs.009.001.09";
        var doc = new XDocument(
            new XElement(XName.Get("Document", nsPacs009),
                new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                new XElement(XName.Get("FICdtTrf", nsPacs009),
                    new XElement(XName.Get("GrpHdr", nsPacs009),
                        new XElement(XName.Get("MsgId", nsPacs009), header.MsgId),
                        new XElement(XName.Get("CreDtTm", nsPacs009), header.CredDtTm),
                        new XElement(XName.Get("NbOfTxs", nsPacs009), details.Count()),
                        new XElement(XName.Get("TtlIntrBkSttlmAmt", nsPacs009), new XAttribute("Ccy", "USD"), details.Sum(d => d.Amount).ToString("F2")),
                        new XElement(XName.Get("IntrBkSttlmDt", nsPacs009), header.InterBankSettlementDate.ToString("yyyy-MM-dd")),
                        new XElement(XName.Get("SttlmInf", nsPacs009),
                            new XElement(XName.Get("ClrSys", nsPacs009),
                                new XElement(XName.Get("Cd", nsPacs009), "RTG")
                            ),
                            new XElement(XName.Get("SttlmMtd", nsPacs009), "CLRG")
                        ),
                        new XElement(XName.Get("InstgAgt", nsPacs009),
                            new XElement(XName.Get("FinInstnId", nsPacs009),
                                new XElement(XName.Get("BICFI", nsPacs009), "SPSOSOS0")
                            )
                        ),
                        new XElement(XName.Get("InstdAgt", nsPacs009),
                            new XElement(XName.Get("FinInstnId", nsPacs009),
                                new XElement(XName.Get("BICFI", nsPacs009), "CBSOSOS0")
                            )
                        )
                    ),
                    details.Select(d =>
                        new XElement(XName.Get("CdtTrfTxInf", nsPacs009),
                            new XElement(XName.Get("PmtId", nsPacs009),
                                new XElement(XName.Get("EndToEndId", nsPacs009), d.EndToEndId),
                                new XElement(XName.Get("TxId", nsPacs009), d.TxId)
                            ),
                            new XElement(XName.Get("PmtTpInf", nsPacs009),
                                new XElement(XName.Get("InstrPrty", nsPacs009), d.InstructionPriority),
                                new XElement(XName.Get("SvcLvl", nsPacs009),
                                    new XElement(XName.Get("Prtry", nsPacs009), d.SvcLvl)
                                ),
                                new XElement(XName.Get("LclInstrm", nsPacs009),
                                    new XElement(XName.Get("Prtry", nsPacs009), d.LocalInstrument)
                                )
                            ),
                            new XElement(XName.Get("IntrBkSttlmAmt", nsPacs009),
                                new XAttribute("Ccy", "USD"),
                                d.Amount.ToString("F2")
                            ),
                            new XElement(XName.Get("Dbtr", nsPacs009),
                                new XElement(XName.Get("FinInstnId", nsPacs009),
                                    new XElement(XName.Get("BICFI", nsPacs009), d.DebtorBic)
                                )
                            ),
                            new XElement(XName.Get("Cdtr", nsPacs009),
                                new XElement(XName.Get("FinInstnId", nsPacs009),
                                    new XElement(XName.Get("BICFI", nsPacs009), d.CreditorBic)
                                )
                            )
                        )
                    )
                )
            )
        );
        var userReference = isFee ? "FR_" + header.OriginalUserReference : "TR_" + header.OriginalUserReference;
        var bizMsgId = isFee ? "FR_" + header.BizMsgId : "TR_" + header.BizMsgId;
        return BuildDataPDU(doc, userReference, header.From, header.To, bizMsgId, "RTGSNetSettlement", header.InterBankSettlementDate);
    }

    private static XDocument BuildDataPDU(
    XDocument pacs009Doc,
    string userReference,
    string From,
    string To,
    string BizMsgId,
    string BizSvc,
    DateTimeOffset CreDt)
    {
        var dataPdu = new XElement(nsDataPdu + "DataPDU",
            new XElement(nsDataPdu + "Revision", "2.0.7"),
            new XElement(nsDataPdu + "Header",
                new XElement(nsDataPdu + "Message",
                    new XElement(nsDataPdu + "SenderReference", userReference),
                    new XElement(nsDataPdu + "MessageIdentifier", "pacs.009.001.09"),
                    new XElement(nsDataPdu + "Format", "AnyXML"),
                    new XElement(nsDataPdu + "Sender",
                        new XElement(nsDataPdu + "BIC12", From + "XXXX"),
                        new XElement(nsDataPdu + "FullName",
                            new XElement(nsDataPdu + "X1", From + "XXX")
                        )
                    ),
                    new XElement(nsDataPdu + "Receiver",
                        new XElement(nsDataPdu + "BIC12", To + "XXXX"),
                        new XElement(nsDataPdu + "FullName",
                            new XElement(nsDataPdu + "X1", To + "XXX")
                        )
                    ),
                    new XElement(nsDataPdu + "InterfaceInfo",
                        new XElement(nsDataPdu + "UserReference", userReference)
                    ),
                    new XElement(nsDataPdu + "NetworkInfo",
                        new XElement(nsDataPdu + "Priority", "Normal"),
                        new XElement(nsDataPdu + "Network", "Application")
                    )
                )
            ),
            new XElement(nsDataPdu + "Body",
                new XElement(nsHead + "AppHdr",
                    new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                    new XElement(nsHead + "Fr",
                        new XElement(nsHead + "FIId",
                            new XElement(nsHead + "FinInstnId",
                                new XElement(nsHead + "BICFI", From)
                            )
                        )
                    ),
                    new XElement(nsHead + "To",
                        new XElement(nsHead + "FIId",
                            new XElement(nsHead + "FinInstnId",
                                new XElement(nsHead + "BICFI", To)
                            )
                        )
                    ),
                    new XElement(nsHead + "BizMsgIdr", BizMsgId),
                    new XElement(nsHead + "MsgDefIdr", "pacs.009.001.09"),
                    new XElement(nsHead + "BizSvc", BizSvc),
                    new XElement(nsHead + "CreDt", CreDt.ToString("yyyy-MM-ddTHH:mm:ss.fffK")),
                    new XElement(nsHead + "Sgntr",
                        new XElement(XName.Get("XMLSgntrs", "http://www.w3.org/2000/09/xmldsig#"))
                    )
                ),
                pacs009Doc.Root
            )
        );

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), dataPdu);
    }
}