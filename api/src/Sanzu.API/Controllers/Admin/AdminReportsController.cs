using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;
using Sanzu.Infrastructure.Data;

namespace Sanzu.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/reports")]
[Authorize(Policy = "AdminFinance")]
public sealed class AdminReportsController : ControllerBase
{
    private readonly SanzuDbContext _dbContext;
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdminReportsController(SanzuDbContext dbContext, IAuditRepository auditRepository, IUnitOfWork unitOfWork)
    {
        _dbContext = dbContext;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpPost("business-summary")]
    [ProducesResponseType(typeof(ApiEnvelope<BusinessSummaryReport>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateBusinessSummary(
        [FromBody] BusinessSummaryRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        var periodStart = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1);

        var orgs = await _dbContext.Organizations.IgnoreQueryFilters()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var activeOrgs = orgs.Where(o => o.Status == TenantStatus.Active).ToList();
        var newSignups = orgs.Count(o => o.CreatedAt >= periodStart && o.CreatedAt < periodEnd);
        var churned = orgs.Count(o => o.Status == TenantStatus.Suspended &&
                                       o.UpdatedAt >= periodStart && o.UpdatedAt < periodEnd);

        var billingRecords = await _dbContext.BillingRecords.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(b => b.BillingCycleStart >= periodStart && b.BillingCycleStart < periodEnd)
            .ToListAsync(cancellationToken);

        var totalRevenue = (double)billingRecords.Sum(b => b.TotalAmount);
        var mrr = totalRevenue;
        var arr = mrr * 12;

        var billingFailed = activeOrgs.Count(o => o.FailedPaymentAttempts > 0);
        var billingOverdue = billingRecords.Count(b => b.Status == "Overdue");

        var alerts = await _dbContext.AdminAlerts
            .AsNoTracking()
            .Where(a => a.FiredAt >= periodStart && a.FiredAt < periodEnd)
            .ToListAsync(cancellationToken);

        var report = new BusinessSummaryReport
        {
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TotalActiveTenants = activeOrgs.Count,
            NewSignups = newSignups,
            ChurnedTenants = churned,
            Mrr = Math.Round(mrr, 2),
            Arr = Math.Round(arr, 2),
            GrowthRate = activeOrgs.Count > 0 ? Math.Round((double)newSignups / activeOrgs.Count * 100, 2) : 0,
            ChurnRate = activeOrgs.Count > 0 ? Math.Round((double)churned / activeOrgs.Count * 100, 2) : 0,
            BillingFailedCount = billingFailed,
            BillingOverdueCount = billingOverdue,
            AlertsFired = alerts.Count,
            AlertsByType = alerts.GroupBy(a => a.AlertType).ToDictionary(g => g.Key, g => g.Count())
        };

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _auditRepository.CreateAsync(new AuditEvent
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                EventType = "Admin.Report.BusinessSummaryGenerated",
                Metadata = JsonSerializer.Serialize(new { month = request.Month, year = request.Year }),
                CreatedAt = DateTime.UtcNow
            }, ct);
        }, cancellationToken);

        return Ok(ApiEnvelope<BusinessSummaryReport>.Success(report, BuildMeta()));
    }

    [HttpPost("compliance-audit")]
    [Authorize(Policy = "AdminFull")]
    [ProducesResponseType(typeof(ApiEnvelope<ComplianceAuditReport>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateComplianceAudit(
        [FromBody] ComplianceAuditRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        var dateFrom = request.DateFrom;
        var dateTo = request.DateTo;

        var events = await _dbContext.AuditEvents
            .AsNoTracking()
            .Where(e => e.CreatedAt >= dateFrom && e.CreatedAt <= dateTo)
            .OrderByDescending(e => e.CreatedAt)
            .Take(10000)
            .ToListAsync(cancellationToken);

        var actorIds = events.Select(e => e.ActorUserId).Distinct().ToList();
        var actors = await _dbContext.Users.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => actorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        var byActor = events
            .GroupBy(e => e.ActorUserId)
            .Select(g => new ComplianceAuditActorGroup
            {
                ActorUserId = g.Key,
                ActorName = actors.GetValueOrDefault(g.Key, "System"),
                EventCount = g.Count(),
                EventTypes = g.GroupBy(e => e.EventType).ToDictionary(eg => eg.Key, eg => eg.Count())
            })
            .OrderByDescending(g => g.EventCount)
            .ToList();

        var report = new ComplianceAuditReport
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            TotalEvents = events.Count,
            UniqueActors = actorIds.Count,
            EventsByType = events.GroupBy(e => e.EventType).ToDictionary(g => g.Key, g => g.Count()),
            ActorGroups = byActor
        };

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _auditRepository.CreateAsync(new AuditEvent
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                EventType = "Admin.Report.ComplianceAuditGenerated",
                Metadata = JsonSerializer.Serialize(new
                {
                    dateFrom = request.DateFrom,
                    dateTo = request.DateTo,
                    totalEvents = report.TotalEvents
                }),
                CreatedAt = DateTime.UtcNow
            }, ct);
        }, cancellationToken);

        return Ok(ApiEnvelope<ComplianceAuditReport>.Success(report, BuildMeta()));
    }

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

public sealed class BusinessSummaryRequest
{
    public int Month { get; set; }
    public int Year { get; set; }
}

public sealed class BusinessSummaryReport
{
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
    public int TotalActiveTenants { get; init; }
    public int NewSignups { get; init; }
    public int ChurnedTenants { get; init; }
    public double Mrr { get; init; }
    public double Arr { get; init; }
    public double GrowthRate { get; init; }
    public double ChurnRate { get; init; }
    public int BillingFailedCount { get; init; }
    public int BillingOverdueCount { get; init; }
    public int AlertsFired { get; init; }
    public Dictionary<string, int> AlertsByType { get; init; } = new();
}

public sealed class ComplianceAuditRequest
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
}

public sealed class ComplianceAuditReport
{
    public DateTime DateFrom { get; init; }
    public DateTime DateTo { get; init; }
    public int TotalEvents { get; init; }
    public int UniqueActors { get; init; }
    public Dictionary<string, int> EventsByType { get; init; } = new();
    public IReadOnlyList<ComplianceAuditActorGroup> ActorGroups { get; init; } = [];
}

public sealed class ComplianceAuditActorGroup
{
    public Guid ActorUserId { get; init; }
    public string ActorName { get; init; } = string.Empty;
    public int EventCount { get; init; }
    public Dictionary<string, int> EventTypes { get; init; } = new();
}
