using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanzu.Core.Entities;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;

namespace Sanzu.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/config")]
[Authorize(Policy = "AdminFull")]
public sealed class AdminConfigController : ControllerBase
{
    private readonly IAdminConfigRepository _configRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly string[] AlertThresholdKeys =
    [
        "alerts.healthScoreRedThreshold",
        "alerts.caseStalledDaysThreshold",
        "alerts.onboardingStalledDaysThreshold",
        "alerts.billingFailedAlertEnabled"
    ];

    private static readonly Dictionary<string, string> AlertThresholdDefaults = new()
    {
        ["alerts.healthScoreRedThreshold"] = "30",
        ["alerts.caseStalledDaysThreshold"] = "14",
        ["alerts.onboardingStalledDaysThreshold"] = "21",
        ["alerts.billingFailedAlertEnabled"] = "true"
    };

    private static readonly string[] HealthWeightKeys =
    [
        "health.billingWeight",
        "health.caseCompletionWeight",
        "health.onboardingWeight"
    ];

    private static readonly Dictionary<string, string> HealthWeightDefaults = new()
    {
        ["health.billingWeight"] = "40",
        ["health.caseCompletionWeight"] = "35",
        ["health.onboardingWeight"] = "25"
    };

    public AdminConfigController(
        IAdminConfigRepository configRepository,
        IAuditRepository auditRepository,
        IUnitOfWork unitOfWork)
    {
        _configRepository = configRepository;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpGet("alerts")]
    [ProducesResponseType(typeof(ApiEnvelope<AlertThresholdsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAlertThresholds(CancellationToken cancellationToken)
    {
        var configs = await _configRepository.GetAllAsync(cancellationToken);
        var lookup = configs.ToDictionary(c => c.ConfigKey, c => c.ConfigValue);

        var response = new AlertThresholdsResponse
        {
            HealthScoreRedThreshold = GetIntValue(lookup, "alerts.healthScoreRedThreshold", 30),
            CaseStalledDaysThreshold = GetIntValue(lookup, "alerts.caseStalledDaysThreshold", 14),
            OnboardingStalledDaysThreshold = GetIntValue(lookup, "alerts.onboardingStalledDaysThreshold", 21),
            BillingFailedAlertEnabled = GetBoolValue(lookup, "alerts.billingFailedAlertEnabled", true)
        };

        return Ok(ApiEnvelope<AlertThresholdsResponse>.Success(response, BuildMeta()));
    }

    [HttpPut("alerts")]
    [ProducesResponseType(typeof(ApiEnvelope<AlertThresholdsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAlertThresholds(
        [FromBody] AlertThresholdsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        // Get before values for audit
        var beforeConfigs = await _configRepository.GetAllAsync(cancellationToken);
        var beforeLookup = beforeConfigs.ToDictionary(c => c.ConfigKey, c => c.ConfigValue);
        var before = new
        {
            healthScoreRedThreshold = GetIntValue(beforeLookup, "alerts.healthScoreRedThreshold", 30),
            caseStalledDaysThreshold = GetIntValue(beforeLookup, "alerts.caseStalledDaysThreshold", 14),
            onboardingStalledDaysThreshold = GetIntValue(beforeLookup, "alerts.onboardingStalledDaysThreshold", 21),
            billingFailedAlertEnabled = GetBoolValue(beforeLookup, "alerts.billingFailedAlertEnabled", true)
        };

        await _configRepository.UpsertAsync("alerts.healthScoreRedThreshold", request.HealthScoreRedThreshold.ToString(), cancellationToken);
        await _configRepository.UpsertAsync("alerts.caseStalledDaysThreshold", request.CaseStalledDaysThreshold.ToString(), cancellationToken);
        await _configRepository.UpsertAsync("alerts.onboardingStalledDaysThreshold", request.OnboardingStalledDaysThreshold.ToString(), cancellationToken);
        await _configRepository.UpsertAsync("alerts.billingFailedAlertEnabled", request.BillingFailedAlertEnabled.ToString(), cancellationToken);

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _auditRepository.CreateAsync(new AuditEvent
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                EventType = "Admin.Config.AlertThresholdsUpdated",
                Metadata = JsonSerializer.Serialize(new
                {
                    before,
                    after = new
                    {
                        healthScoreRedThreshold = request.HealthScoreRedThreshold,
                        caseStalledDaysThreshold = request.CaseStalledDaysThreshold,
                        onboardingStalledDaysThreshold = request.OnboardingStalledDaysThreshold,
                        billingFailedAlertEnabled = request.BillingFailedAlertEnabled
                    }
                }),
                CreatedAt = DateTime.UtcNow
            }, ct);
        }, cancellationToken);

        var response = new AlertThresholdsResponse
        {
            HealthScoreRedThreshold = request.HealthScoreRedThreshold,
            CaseStalledDaysThreshold = request.CaseStalledDaysThreshold,
            OnboardingStalledDaysThreshold = request.OnboardingStalledDaysThreshold,
            BillingFailedAlertEnabled = request.BillingFailedAlertEnabled
        };

        return Ok(ApiEnvelope<AlertThresholdsResponse>.Success(response, BuildMeta()));
    }

    [HttpGet("health-weights")]
    [ProducesResponseType(typeof(ApiEnvelope<HealthWeightsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHealthWeights(CancellationToken cancellationToken)
    {
        var configs = await _configRepository.GetAllAsync(cancellationToken);
        var lookup = configs.ToDictionary(c => c.ConfigKey, c => c.ConfigValue);

        var response = new HealthWeightsResponse
        {
            BillingWeight = GetIntValue(lookup, "health.billingWeight", 40),
            CaseCompletionWeight = GetIntValue(lookup, "health.caseCompletionWeight", 35),
            OnboardingWeight = GetIntValue(lookup, "health.onboardingWeight", 25)
        };

        return Ok(ApiEnvelope<HealthWeightsResponse>.Success(response, BuildMeta()));
    }

    [HttpPut("health-weights")]
    [ProducesResponseType(typeof(ApiEnvelope<HealthWeightsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateHealthWeights(
        [FromBody] HealthWeightsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        if (request.BillingWeight + request.CaseCompletionWeight + request.OnboardingWeight != 100)
            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "Weights must sum to 100" });

        var beforeConfigs = await _configRepository.GetAllAsync(cancellationToken);
        var beforeLookup = beforeConfigs.ToDictionary(c => c.ConfigKey, c => c.ConfigValue);
        var before = new
        {
            billingWeight = GetIntValue(beforeLookup, "health.billingWeight", 40),
            caseCompletionWeight = GetIntValue(beforeLookup, "health.caseCompletionWeight", 35),
            onboardingWeight = GetIntValue(beforeLookup, "health.onboardingWeight", 25)
        };

        await _configRepository.UpsertAsync("health.billingWeight", request.BillingWeight.ToString(), cancellationToken);
        await _configRepository.UpsertAsync("health.caseCompletionWeight", request.CaseCompletionWeight.ToString(), cancellationToken);
        await _configRepository.UpsertAsync("health.onboardingWeight", request.OnboardingWeight.ToString(), cancellationToken);

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _auditRepository.CreateAsync(new AuditEvent
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                EventType = "Admin.Config.HealthWeightsUpdated",
                Metadata = JsonSerializer.Serialize(new
                {
                    before,
                    after = new
                    {
                        billingWeight = request.BillingWeight,
                        caseCompletionWeight = request.CaseCompletionWeight,
                        onboardingWeight = request.OnboardingWeight
                    }
                }),
                CreatedAt = DateTime.UtcNow
            }, ct);
        }, cancellationToken);

        var response = new HealthWeightsResponse
        {
            BillingWeight = request.BillingWeight,
            CaseCompletionWeight = request.CaseCompletionWeight,
            OnboardingWeight = request.OnboardingWeight
        };

        return Ok(ApiEnvelope<HealthWeightsResponse>.Success(response, BuildMeta()));
    }

    private static int GetIntValue(Dictionary<string, string> lookup, string key, int defaultValue)
        => lookup.TryGetValue(key, out var value) && int.TryParse(value, out var result) ? result : defaultValue;

    private static bool GetBoolValue(Dictionary<string, string> lookup, string key, bool defaultValue)
        => lookup.TryGetValue(key, out var value) && bool.TryParse(value, out var result) ? result : defaultValue;

    private bool TryGetActorUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                    ?? User.FindFirst("sub")
                    ?? User.FindFirst("user_id");
        return claim != null && Guid.TryParse(claim.Value, out userId);
    }

    private static Dictionary<string, object?> BuildMeta()
        => new() { ["timestamp"] = DateTime.UtcNow };
}

public sealed class AlertThresholdsResponse
{
    public int HealthScoreRedThreshold { get; init; }
    public int CaseStalledDaysThreshold { get; init; }
    public int OnboardingStalledDaysThreshold { get; init; }
    public bool BillingFailedAlertEnabled { get; init; }
}

public sealed class AlertThresholdsRequest
{
    public int HealthScoreRedThreshold { get; set; }
    public int CaseStalledDaysThreshold { get; set; }
    public int OnboardingStalledDaysThreshold { get; set; }
    public bool BillingFailedAlertEnabled { get; set; }
}

public sealed class HealthWeightsResponse
{
    public int BillingWeight { get; init; }
    public int CaseCompletionWeight { get; init; }
    public int OnboardingWeight { get; init; }
}

public sealed class HealthWeightsRequest
{
    public int BillingWeight { get; set; }
    public int CaseCompletionWeight { get; set; }
    public int OnboardingWeight { get; set; }
}
