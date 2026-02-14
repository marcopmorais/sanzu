using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.API.Controllers;

[ApiController]
[Route("api/v1/tenants/{tenantId:guid}/glossary")]
[Authorize]
public sealed class GlossaryController : ControllerBase
{
    private readonly IGlossaryService _glossaryService;

    public GlossaryController(IGlossaryService glossaryService)
    {
        _glossaryService = glossaryService;
    }

    /// <summary>
    /// Search glossary terms by query
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiEnvelope<GlossaryLookupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search(
        Guid tenantId,
        [FromQuery] string? q,
        [FromQuery] string? locale,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        var response = await _glossaryService.SearchAsync(
            tenantId,
            actorUserId,
            q,
            locale,
            cancellationToken);

        var meta = new Dictionary<string, object?>
        {
            ["timestamp"] = DateTime.UtcNow,
            ["query"] = q,
            ["locale"] = locale ?? "pt-PT"
        };

        return Ok(ApiEnvelope<GlossaryLookupResponse>.Success(response, meta));
    }

    /// <summary>
    /// Get a specific glossary term by key
    /// </summary>
    [HttpGet("{key}")]
    [ProducesResponseType(typeof(ApiEnvelope<GlossaryTermResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTerm(
        Guid tenantId,
        string key,
        [FromQuery] string? locale,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _glossaryService.GetTermAsync(
                tenantId,
                actorUserId,
                key,
                locale,
                cancellationToken);

            var meta = new Dictionary<string, object?>
            {
                ["timestamp"] = DateTime.UtcNow
            };

            return Ok(ApiEnvelope<GlossaryTermResponse>.Success(response, meta));
        }
        catch (CaseStateException)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Glossary term not found",
                Status = StatusCodes.Status404NotFound,
                Detail = "The requested glossary term does not exist or is not visible to you."
            });
        }
    }

    /// <summary>
    /// Create or update a glossary term (Agency Admin only)
    /// </summary>
    [HttpPut("{key}")]
    [Authorize(Policy = "TenantAdmin")]
    [ProducesResponseType(typeof(ApiEnvelope<GlossaryTermResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpsertTerm(
        Guid tenantId,
        string key,
        [FromBody] UpsertGlossaryTermRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _glossaryService.UpsertAsync(
                tenantId,
                actorUserId,
                key,
                request,
                cancellationToken);

            var meta = new Dictionary<string, object?>
            {
                ["timestamp"] = DateTime.UtcNow
            };

            return Ok(ApiEnvelope<GlossaryTermResponse>.Success(response, meta));
        }
        catch (ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(
                    x => string.IsNullOrWhiteSpace(x.PropertyName)
                        ? "request"
                        : x.PropertyName)
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(e => e.ErrorMessage).Distinct().ToArray());

            var details = new ValidationProblemDetails(errors)
            {
                Title = "Invalid glossary term request",
                Status = StatusCodes.Status400BadRequest
            };

            return BadRequest(details);
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
    }

    private bool TryGetActorUserId(out Guid actorUserId)
    {
        var userIdClaim = User.FindFirst("sub")?.Value
                          ?? User.FindFirst("userId")?.Value;

        if (userIdClaim is not null && Guid.TryParse(userIdClaim, out actorUserId))
        {
            return true;
        }

        actorUserId = Guid.Empty;
        return false;
    }
}
