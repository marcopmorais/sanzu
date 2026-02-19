using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanzu.API.Filters;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/team")]
[Authorize(Policy = "AdminFull")]
public sealed class AdminTeamController : ControllerBase
{
    private readonly IAdminTeamService _adminTeamService;
    private readonly IValidator<GrantAdminRoleRequest> _grantValidator;

    public AdminTeamController(
        IAdminTeamService adminTeamService,
        IValidator<GrantAdminRoleRequest> grantValidator)
    {
        _adminTeamService = adminTeamService;
        _grantValidator = grantValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<AdminTeamMemberResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTeamMembers(CancellationToken cancellationToken)
    {
        var members = await _adminTeamService.ListTeamMembersAsync(cancellationToken);

        return Ok(ApiEnvelope<IReadOnlyList<AdminTeamMemberResponse>>.Success(
            members,
            new Dictionary<string, object?> { ["timestamp"] = DateTime.UtcNow }));
    }

    [HttpPost("{userId:guid}/roles")]
    [SkipAdminAudit]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GrantRole(
        Guid userId,
        [FromBody] GrantAdminRoleRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _grantValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)),
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            await _adminTeamService.GrantRoleAsync(userId, request.Role, actorUserId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Grant role failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpDelete("{userId:guid}/roles/{role}")]
    [SkipAdminAudit]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RevokeRole(
        Guid userId,
        string role,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            await _adminTeamService.RevokeRoleAsync(userId, role, actorUserId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Revoke role failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
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
}
