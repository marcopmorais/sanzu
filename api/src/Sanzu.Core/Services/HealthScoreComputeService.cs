using System.Text.Json;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Services;

public sealed class HealthScoreComputeService : IHealthScoreComputeService
{
    private readonly IEnumerable<IHealthScoreInput> _inputs;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ITenantHealthScoreRepository _healthScoreRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;

    public HealthScoreComputeService(
        IEnumerable<IHealthScoreInput> inputs,
        IOrganizationRepository organizationRepository,
        ITenantHealthScoreRepository healthScoreRepository,
        IAuditRepository auditRepository,
        IUnitOfWork unitOfWork)
    {
        _inputs = inputs;
        _organizationRepository = organizationRepository;
        _healthScoreRepository = healthScoreRepository;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task ComputeForAllTenantsAsync(CancellationToken cancellationToken)
    {
        var tenants = await _organizationRepository.GetAllAsync(cancellationToken);
        var activeTenants = tenants.Where(t => t.Status == TenantStatus.Active).ToList();

        var greenCount = 0;
        var yellowCount = 0;
        var redCount = 0;

        foreach (var tenant in activeTenants)
        {
            var score = await ComputeForTenantAsync(tenant.Id, cancellationToken);

            switch (score.HealthBand)
            {
                case HealthBand.Green: greenCount++; break;
                case HealthBand.Yellow: yellowCount++; break;
                case HealthBand.Red: redCount++; break;
            }
        }

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var auditEvent = new AuditEvent
                {
                    Id = Guid.NewGuid(),
                    ActorUserId = Guid.Empty,
                    EventType = "Admin.HealthScore.Computed",
                    Metadata = JsonSerializer.Serialize(new
                    {
                        TenantsScored = activeTenants.Count,
                        Green = greenCount,
                        Yellow = yellowCount,
                        Red = redCount
                    })
                };

                await _auditRepository.CreateAsync(auditEvent, token);
            },
            cancellationToken);
    }

    public async Task<IReadOnlyList<TenantHealthScoreResponse>> GetLatestScoresAsync(
        CancellationToken cancellationToken)
    {
        var scores = await _healthScoreRepository.GetLatestForAllTenantsAsync(cancellationToken);
        var tenants = await _organizationRepository.GetAllAsync(cancellationToken);
        var tenantMap = tenants.ToDictionary(t => t.Id, t => t.Name);

        return scores.Select(s => new TenantHealthScoreResponse
        {
            Id = s.Id,
            TenantId = s.TenantId,
            TenantName = tenantMap.GetValueOrDefault(s.TenantId, "Unknown"),
            OverallScore = s.OverallScore,
            BillingScore = s.BillingScore,
            CaseCompletionScore = s.CaseCompletionScore,
            OnboardingScore = s.OnboardingScore,
            HealthBand = s.HealthBand,
            PrimaryIssue = s.PrimaryIssue,
            ComputedAt = s.ComputedAt
        }).ToList();
    }

    private async Task<TenantHealthScore> ComputeForTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var inputList = _inputs.ToList();
        var results = new List<(IHealthScoreInput Input, HealthScoreInputResult Result)>();

        foreach (var input in inputList)
        {
            var result = await input.ComputeAsync(tenantId, cancellationToken);
            results.Add((input, result));
        }

        var totalWeight = results.Sum(r => r.Input.Weight);
        var weightedSum = results.Sum(r => r.Input.Weight * r.Result.Score);
        var weightedAverage = totalWeight > 0 ? (int)Math.Round((double)weightedSum / totalWeight) : 0;

        // Apply FloorCap logic
        var floorCaps = results
            .Where(r => r.Result.FloorCap.HasValue)
            .Select(r => (r.Result.FloorCap!.Value, r.Result.FloorReason))
            .ToList();

        var overallScore = weightedAverage;
        string? primaryIssue = null;

        if (floorCaps.Count > 0)
        {
            var lowestFloor = floorCaps.MinBy(f => f.Value);
            if (lowestFloor.Value < overallScore)
            {
                overallScore = lowestFloor.Value;
                primaryIssue = lowestFloor.FloorReason;
            }
        }

        overallScore = Math.Clamp(overallScore, 0, 100);

        var billingResult = results.FirstOrDefault(r => r.Input.Name == "Billing");
        var caseResult = results.FirstOrDefault(r => r.Input.Name == "CaseCompletion");
        var onboardingResult = results.FirstOrDefault(r => r.Input.Name == "Onboarding");

        var healthBand = overallScore switch
        {
            >= 70 => HealthBand.Green,
            >= 40 => HealthBand.Yellow,
            _ => HealthBand.Red
        };

        var score = new TenantHealthScore
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OverallScore = overallScore,
            BillingScore = billingResult.Result?.Score ?? 0,
            CaseCompletionScore = caseResult.Result?.Score ?? 0,
            OnboardingScore = onboardingResult.Result?.Score ?? 0,
            HealthBand = healthBand,
            PrimaryIssue = primaryIssue,
            ComputedAt = DateTime.UtcNow
        };

        await _healthScoreRepository.CreateAsync(score, cancellationToken);
        return score;
    }
}
