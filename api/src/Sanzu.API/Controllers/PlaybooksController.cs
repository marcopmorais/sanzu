using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.API.Controllers;

[ApiController]
[Route("api/v1/tenants/{tenantId:guid}/settings/playbooks")]
[Authorize]
public sealed class PlaybooksController : ControllerBase
{
    private readonly IAgencyPlaybookService _playbookService;

    public PlaybooksController(IAgencyPlaybookService playbookService)
    {
        _playbookService = playbookService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<PlaybookResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(Guid tenantId, CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        var response = await _playbookService.ListAsync(tenantId, actorUserId, cancellationToken);

        var meta = new Dictionary<string, object?>
        {
            ["timestamp"] = DateTime.UtcNow
        };

        return Ok(ApiEnvelope<IReadOnlyList<PlaybookResponse>>.Success(response, meta));
    }

    [HttpPost]
    [Authorize(Policy = "TenantAdmin")]
    [ProducesResponseType(typeof(ApiEnvelope<PlaybookResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        Guid tenantId,
        [FromBody] CreatePlaybookRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _playbookService.CreateAsync(tenantId, actorUserId, request, cancellationToken);

            var meta = new Dictionary<string, object?>
            {
                ["timestamp"] = DateTime.UtcNow
            };

            return CreatedAtAction(
                nameof(GetById),
                new { tenantId, playbookId = response.Id },
                ApiEnvelope<PlaybookResponse>.Success(response, meta));
        }
        catch (ValidationException ex)
        {
            return BadRequest(ToValidationProblem(ex));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
    }

    [HttpGet("{playbookId:guid}")]
    [ProducesResponseType(typeof(ApiEnvelope<PlaybookResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid tenantId,
        Guid playbookId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _playbookService.GetByIdAsync(tenantId, actorUserId, playbookId, cancellationToken);

            var meta = new Dictionary<string, object?>
            {
                ["timestamp"] = DateTime.UtcNow
            };

            return Ok(ApiEnvelope<PlaybookResponse>.Success(response, meta));
        }
        catch (CaseStateException)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Playbook not found",
                Status = StatusCodes.Status404NotFound,
                Detail = "The requested playbook does not exist."
            });
        }
    }

    [HttpPatch("{playbookId:guid}")]
    [Authorize(Policy = "TenantAdmin")]
    [ProducesResponseType(typeof(ApiEnvelope<PlaybookResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid tenantId,
        Guid playbookId,
        [FromBody] UpdatePlaybookRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _playbookService.UpdateAsync(tenantId, actorUserId, playbookId, request, cancellationToken);

            var meta = new Dictionary<string, object?>
            {
                ["timestamp"] = DateTime.UtcNow
            };

            return Ok(ApiEnvelope<PlaybookResponse>.Success(response, meta));
        }
        catch (ValidationException ex)
        {
            return BadRequest(ToValidationProblem(ex));
        }
        catch (CaseStateException ex)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Invalid playbook state",
                Status = StatusCodes.Status422UnprocessableEntity,
                Detail = ex.Message
            });
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
    }

    [HttpPost("{playbookId:guid}/activate")]
    [Authorize(Policy = "TenantAdmin")]
    [ProducesResponseType(typeof(ApiEnvelope<PlaybookResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(
        Guid tenantId,
        Guid playbookId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _playbookService.ActivateAsync(tenantId, actorUserId, playbookId, cancellationToken);

            var meta = new Dictionary<string, object?>
            {
                ["timestamp"] = DateTime.UtcNow
            };

            return Ok(ApiEnvelope<PlaybookResponse>.Success(response, meta));
        }
        catch (CaseStateException)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Playbook not found",
                Status = StatusCodes.Status404NotFound,
                Detail = "The requested playbook does not exist."
            });
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
    }

    private bool TryGetActorUserId(out Guid actorUserId)
    {
        actorUserId = Guid.Empty;
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("user_id");

        return Guid.TryParse(userIdValue, out actorUserId);
    }

    private static ValidationProblemDetails ToValidationProblem(ValidationException ex)
    {
        var errors = ex.Errors
            .GroupBy(x => string.IsNullOrWhiteSpace(x.PropertyName) ? "request" : x.PropertyName)
            .ToDictionary(x => x.Key, x => x.Select(e => e.ErrorMessage).Distinct().ToArray());

        return new ValidationProblemDetails(errors)
        {
            Title = "Invalid playbook request",
            Status = StatusCodes.Status400BadRequest
        };
    }
}
