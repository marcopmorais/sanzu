using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface ICaseAuditExportService
{
    Task<CaseAuditExportResponse> ExportAsync(
        Guid tenantId,
        Guid caseId,
        Guid actorUserId,
        CancellationToken cancellationToken);
}
