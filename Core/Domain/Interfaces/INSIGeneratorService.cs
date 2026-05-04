using System.Threading;
using System.Threading.Tasks;

namespace SPSS.Domain.Interfaces;

public interface INSIGeneratorService
{
    Task<bool> GenerateRTGSFilesAsync(long headerId, string outputDirectory, string feeTtc, string txnTtc, CancellationToken ct);
}
