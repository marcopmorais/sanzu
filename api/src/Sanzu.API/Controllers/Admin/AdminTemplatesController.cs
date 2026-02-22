using System.Security.Claims;
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
[Route("api/v1/admin/config/templates")]
[Authorize(Policy = "AdminFull")]
public sealed class AdminTemplatesController : ControllerBase
{
    private readonly SanzuDbContext _dbContext;
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdminTemplatesController(SanzuDbContext dbContext, IAuditRepository auditRepository, IUnitOfWork unitOfWork)
    {
        _dbContext = dbContext;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<TemplateResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTemplates(CancellationToken cancellationToken)
    {
        var templates = await _dbContext.CommunicationTemplates
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TemplateResponse
            {
                Id = t.Id,
                Name = t.Name,
                Subject = t.Subject,
                Body = t.Body,
                MessageType = t.MessageType,
                LastUpdated = t.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiEnvelope<IReadOnlyList<TemplateResponse>>.Success(templates, BuildMeta()));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiEnvelope<TemplateResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTemplate(
        [FromBody] TemplateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Subject))
            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "Name and Subject are required" });

        var template = new CommunicationTemplate
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Subject = request.Subject,
            Body = request.Body ?? "",
            MessageType = request.MessageType ?? "support",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.CommunicationTemplates.Add(template);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _auditRepository.CreateAsync(new AuditEvent
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                EventType = "Admin.Config.TemplateCreated",
                Metadata = JsonSerializer.Serialize(new
                {
                    templateId = template.Id,
                    name = template.Name,
                    messageType = template.MessageType
                }),
                CreatedAt = DateTime.UtcNow
            }, ct);
        }, cancellationToken);

        var response = new TemplateResponse
        {
            Id = template.Id,
            Name = template.Name,
            Subject = template.Subject,
            Body = template.Body,
            MessageType = template.MessageType,
            LastUpdated = template.UpdatedAt
        };

        return Created($"/api/v1/admin/config/templates/{template.Id}",
            ApiEnvelope<TemplateResponse>.Success(response, BuildMeta()));
    }

    [HttpPut("{templateId:guid}")]
    [ProducesResponseType(typeof(ApiEnvelope<TemplateResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTemplate(
        Guid templateId,
        [FromBody] TemplateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        var template = await _dbContext.CommunicationTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

        if (template is null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = "Template not found" });

        template.Name = request.Name ?? template.Name;
        template.Subject = request.Subject ?? template.Subject;
        template.Body = request.Body ?? template.Body;
        template.MessageType = request.MessageType ?? template.MessageType;
        template.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _auditRepository.CreateAsync(new AuditEvent
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                EventType = "Admin.Config.TemplateUpdated",
                Metadata = JsonSerializer.Serialize(new
                {
                    templateId = template.Id,
                    name = template.Name,
                    messageType = template.MessageType
                }),
                CreatedAt = DateTime.UtcNow
            }, ct);
        }, cancellationToken);

        var response = new TemplateResponse
        {
            Id = template.Id,
            Name = template.Name,
            Subject = template.Subject,
            Body = template.Body,
            MessageType = template.MessageType,
            LastUpdated = template.UpdatedAt
        };

        return Ok(ApiEnvelope<TemplateResponse>.Success(response, BuildMeta()));
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

public sealed class TemplateRequest
{
    public string? Name { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string? MessageType { get; set; }
}

public sealed class TemplateResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string MessageType { get; init; } = string.Empty;
    public DateTime LastUpdated { get; init; }
}
