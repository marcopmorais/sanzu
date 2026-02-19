using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface IPlatformSummaryService
{
    Task<PlatformOperationsSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken);
}
