using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;

namespace Sanzu.API.Controllers;

[ApiController]
[Route("api/v1/admin/queues")]
public sealed class AdminQueueController : ControllerBase
{
    private readonly IAdminQueueService _queueService;

    public AdminQueueController(IAdminQueueService queueService)
    {
        _queueService = queueService;
    }

    [Authorize(Policy = "SanzuAdmin")]
    [HttpGet]
    [ProducesResponseType(typeof(ApiEnvelope<AdminQueueListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListQueues(CancellationToken cancellationToken = default)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        try
        {
            var response = await _queueService.ListQueuesAsync(actorUserId, cancellationToken);
            return Ok(ApiEnvelope<AdminQueueListResponse>.Success(response, BuildMeta()));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
    }

    [Authorize(Policy = "SanzuAdmin")]
    [HttpGet("{queueId}")]
    [ProducesResponseType(typeof(ApiEnvelope<AdminQueueItemsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetQueueItems(
        string queueId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        try
        {
            var response = await _queueService.GetQueueItemsAsync(actorUserId, queueId, cancellationToken);
            return Ok(ApiEnvelope<AdminQueueItemsResponse>.Success(response, BuildMeta()));
        }
        catch (CaseStateException exception)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Queue error", detail: exception.Message);
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
    }

    [Authorize(Policy = "SanzuAdmin")]
    [HttpGet("events/{tenantId:guid}")]
    [ProducesResponseType(typeof(ApiEnvelope<AdminEventStreamResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetTenantEventStream(
        Guid tenantId,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        try
        {
            var response = await _queueService.GetTenantEventStreamAsync(
                actorUserId, tenantId, limit, cancellationToken);
            return Ok(ApiEnvelope<AdminEventStreamResponse>.Success(response, BuildMeta()));
        }
        catch (CaseStateException exception)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Event stream error", detail: exception.Message);
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
    }

    private bool TryGetActorUserId(out Guid actorUserId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out actorUserId);
    }

    private object BuildMeta() => new { requestId = HttpContext.TraceIdentifier, timestampUtc = DateTime.UtcNow };
}
