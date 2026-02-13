using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface IKpiAlertService
{
    Task<KpiThresholdResponse> UpsertThresholdAsync(
        Guid actorUserId,
        UpsertKpiThresholdRequest request,
        CancellationToken cancellationToken);

    Task<KpiAlertEvaluationResponse> EvaluateThresholdsAsync(
        Guid actorUserId,
        EvaluateKpiAlertsRequest request,
        CancellationToken cancellationToken);
}
