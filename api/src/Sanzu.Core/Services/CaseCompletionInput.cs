using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;

namespace Sanzu.Core.Services;

public sealed class CaseCompletionInput : IHealthScoreInput
{
    private readonly ICaseRepository _caseRepository;

    public CaseCompletionInput(ICaseRepository caseRepository)
    {
        _caseRepository = caseRepository;
    }

    public string Name => "CaseCompletion";
    public int Weight => 35;

    public async Task<HealthScoreInputResult> ComputeAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var cases = await _caseRepository.GetByTenantIdForPlatformAsync(tenantId, cancellationToken);
        if (cases.Count == 0)
        {
            return new HealthScoreInputResult { Score = 50 };
        }

        var closed = cases.Count(c => c.Status is CaseStatus.Closed or CaseStatus.Archived);
        var total = cases.Count;
        var ratio = (double)closed / total;
        var score = (int)Math.Round(ratio * 100);

        return new HealthScoreInputResult { Score = Math.Clamp(score, 0, 100) };
    }
}
