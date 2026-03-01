using Microsoft.Extensions.Logging;
using SPSS.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace SPSS.Infrastructure.Services;

public class NSIGeneratorService : INSIGeneratorService
{
    private readonly IStorageBroker _db;
    private readonly ILogger<NSIGeneratorService> _logger;

    public NSIGeneratorService(IStorageBroker db, ILogger<NSIGeneratorService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<bool> GenerateRTGSFilesAsync(long headerId, string outputDir, string feeTtc, string txnTtc, CancellationToken ct)
    {
        return await NSIService.GenerateRTGSFiles(_db, headerId, outputDir, feeTtc, txnTtc, _logger, ct);
    }
}
