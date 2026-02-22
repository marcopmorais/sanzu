using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;
using Sanzu.Infrastructure.Data;

namespace Sanzu.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/audit")]
[Authorize(Policy = "AdminFinance")]
public sealed class AdminAuditController : ControllerBase
{
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SanzuDbContext _dbContext;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public AdminAuditController(IAuditRepository auditRepository, IUnitOfWork unitOfWork, SanzuDbContext dbContext)
    {
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiEnvelope<AuditSearchResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] Guid? actorUserId = null,
        [FromQuery] string? eventType = null,
        [FromQuery] Guid? caseId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? cursor = null,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 200) pageSize = 200;

        var result = await _auditRepository.SearchAsync(
            actorUserId, eventType, caseId, dateFrom, dateTo, cursor, pageSize, cancellationToken);

        var actorNames = await ResolveActorNamesAsync(result.Items, cancellationToken);

        var response = new AuditSearchResponse
        {
            Items = result.Items.Select(e => MapToResponse(e, actorNames)).ToList(),
            NextCursor = result.NextCursor,
            TotalCount = result.TotalCount
        };

        return Ok(ApiEnvelope<AuditSearchResponse>.Success(response, BuildMeta()));
    }

    [HttpGet("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Export(
        [FromQuery] string format = "csv",
        [FromQuery] Guid? actorUserId = null,
        [FromQuery] string? eventType = null,
        [FromQuery] Guid? caseId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _auditRepository.SearchAsync(
            actorUserId, eventType, caseId, dateFrom, dateTo, null, 10000, cancellationToken);

        var actorNames = await ResolveActorNamesAsync(result.Items, cancellationToken);

        // Log audit export event
        if (TryGetActorUserId(out var exportActorId))
        {
            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await _auditRepository.CreateAsync(new AuditEvent
                {
                    Id = Guid.NewGuid(),
                    ActorUserId = exportActorId,
                    EventType = "Admin.Audit.Exported",
                    Metadata = JsonSerializer.Serialize(new
                    {
                        format,
                        actorUserId,
                        eventType,
                        caseId,
                        dateFrom,
                        dateTo,
                        rowCount = result.Items.Count
                    }, JsonOptions),
                    CreatedAt = DateTime.UtcNow
                }, ct);
            }, cancellationToken);
        }

        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            var items = result.Items.Select(e => MapToResponse(e, actorNames)).ToList();
            var json = JsonSerializer.Serialize(items, JsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);
            return File(bytes, "application/json", "audit-export.json");
        }

        return File(BuildCsv(result.Items, actorNames), "text/csv", "audit-export.csv");
    }

    private async Task<Dictionary<Guid, string>> ResolveActorNamesAsync(
        IReadOnlyList<AuditEvent> events, CancellationToken cancellationToken)
    {
        var actorIds = events.Select(e => e.ActorUserId).Where(id => id != Guid.Empty).Distinct().ToList();
        if (actorIds.Count == 0) return new Dictionary<Guid, string>();

        return await _dbContext.Users
            .IgnoreQueryFilters()
            .Where(u => actorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);
    }

    private static AuditEventResponse MapToResponse(AuditEvent e, Dictionary<Guid, string> actorNames) => new()
    {
        Id = e.Id,
        ActorUserId = e.ActorUserId,
        ActorName = e.ActorUserId == Guid.Empty ? "System" : actorNames.GetValueOrDefault(e.ActorUserId, "Unknown"),
        EventType = e.EventType,
        CaseId = e.CaseId,
        Metadata = e.Metadata,
        Timestamp = e.CreatedAt
    };

    private static byte[] BuildCsv(IReadOnlyList<AuditEvent> items, Dictionary<Guid, string> actorNames)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,ActorUserId,ActorName,EventType,CaseId,Metadata,Timestamp");
        foreach (var e in items)
        {
            var actorName = e.ActorUserId == Guid.Empty ? "System" : actorNames.GetValueOrDefault(e.ActorUserId, "Unknown");
            sb.AppendLine(string.Join(",",
                e.Id,
                e.ActorUserId,
                CsvEscape(actorName),
                CsvEscape(e.EventType),
                e.CaseId?.ToString() ?? "",
                CsvEscape(e.Metadata),
                e.CreatedAt.ToString("O")));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
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
