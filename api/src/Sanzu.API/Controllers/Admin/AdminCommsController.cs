using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;
using Sanzu.Infrastructure.Data;

namespace Sanzu.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = "AdminSupport")]
public sealed class AdminCommsController : ControllerBase
{
    private readonly SanzuDbContext _dbContext;
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdminCommsController(SanzuDbContext dbContext, IAuditRepository auditRepository, IUnitOfWork unitOfWork)
    {
        _dbContext = dbContext;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpPost("tenants/{tenantId:guid}/actions/send-communication")]
    [Authorize(Policy = "AdminOps")]
    [ProducesResponseType(typeof(ApiEnvelope<CommResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> SendCommunication(
        Guid tenantId,
        [FromBody] SendCommunicationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        var tenant = await _dbContext.Organizations.IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == tenantId, cancellationToken);
        if (tenant is null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = "Tenant not found" });

        var comm = new TenantCommunication
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SenderUserId = actorUserId,
            MessageType = request.MessageType ?? "support",
            Subject = request.Subject ?? "",
            Body = request.Body ?? "",
            TemplateId = request.TemplateId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.TenantCommunications.Add(comm);

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _auditRepository.CreateAsync(new AuditEvent
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                EventType = "Admin.Tenant.CommunicationSent",
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    tenantId,
                    messageType = comm.MessageType,
                    subject = comm.Subject,
                    templateId = comm.TemplateId
                }),
                CreatedAt = DateTime.UtcNow
            }, ct);
        }, cancellationToken);

        var response = new CommResponse
        {
            Id = comm.Id,
            TenantId = comm.TenantId,
            MessageType = comm.MessageType,
            Subject = comm.Subject,
            CreatedAt = comm.CreatedAt
        };

        return Created($"/api/v1/admin/tenants/{tenantId}/comms/{comm.Id}",
            ApiEnvelope<CommResponse>.Success(response, BuildMeta()));
    }

    [HttpGet("tenants/{tenantId:guid}/comms")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<CommResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenantComms(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var comms = await _dbContext.TenantCommunications
            .Where(c => c.TenantId == tenantId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CommResponse
            {
                Id = c.Id,
                TenantId = c.TenantId,
                SenderUserId = c.SenderUserId,
                SenderName = c.Sender != null ? c.Sender.FullName : "Unknown",
                MessageType = c.MessageType,
                Subject = c.Subject,
                Body = c.Body,
                CreatedAt = c.CreatedAt
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return Ok(ApiEnvelope<IReadOnlyList<CommResponse>>.Success(comms, BuildMeta()));
    }

    [HttpGet("comms")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<CommResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchComms(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] string? type = null,
        [FromQuery] Guid? senderId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TenantCommunications.AsNoTracking();

        if (tenantId.HasValue)
            query = query.Where(c => c.TenantId == tenantId.Value);
        if (!string.IsNullOrEmpty(type))
            query = query.Where(c => c.MessageType == type);
        if (senderId.HasValue)
            query = query.Where(c => c.SenderUserId == senderId.Value);
        if (dateFrom.HasValue)
            query = query.Where(c => c.CreatedAt >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(c => c.CreatedAt <= dateTo.Value);

        var comms = await query
            .OrderByDescending(c => c.CreatedAt)
            .Take(200)
            .Select(c => new CommResponse
            {
                Id = c.Id,
                TenantId = c.TenantId,
                SenderUserId = c.SenderUserId,
                SenderName = c.Sender != null ? c.Sender.FullName : "Unknown",
                MessageType = c.MessageType,
                Subject = c.Subject,
                Body = c.Body,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiEnvelope<IReadOnlyList<CommResponse>>.Success(comms, BuildMeta()));
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

public sealed class SendCommunicationRequest
{
    public string? TemplateId { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string? MessageType { get; set; }
}

public sealed class CommResponse
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid SenderUserId { get; init; }
    public string SenderName { get; init; } = string.Empty;
    public string MessageType { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
