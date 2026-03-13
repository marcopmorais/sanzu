using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/tenants")]
[Authorize(Policy = "AdminViewer")]
public sealed class AdminTenantsController : ControllerBase
{
    private readonly IAdminTenantService _tenantService;

    public AdminTenantsController(IAdminTenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiEnvelope<PaginatedResponse<TenantListItemResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTenants(
        [FromQuery] TenantListRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _tenantService.ListTenantsAsync(request, cancellationToken);
        return Ok(ApiEnvelope<PaginatedResponse<TenantListItemResponse>>.Success(result, BuildMeta()));
    }

    [HttpGet("{tenantId:guid}/summary")]
    [ProducesResponseType(typeof(ApiEnvelope<TenantSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantSummary(Guid tenantId, CancellationToken cancellationToken)
    {
        var result = await _tenantService.GetTenantSummaryAsync(tenantId, cancellationToken);
        if (result is null)
            return NotFound(new ProblemDetails { Title = "Tenant not found", Status = 404 });

        return Ok(ApiEnvelope<TenantSummaryResponse>.Success(result, BuildMeta()));
    }

    [HttpGet("{tenantId:guid}/billing")]
    [Authorize(Policy = "AdminFinance")]
    [ProducesResponseType(typeof(ApiEnvelope<TenantBillingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantBilling(Guid tenantId, CancellationToken cancellationToken)
    {
        var result = await _tenantService.GetTenantBillingAsync(tenantId, cancellationToken);
        if (result is null)
            return NotFound(new ProblemDetails { Title = "Tenant not found", Status = 404 });

        return Ok(ApiEnvelope<TenantBillingResponse>.Success(result, BuildMeta()));
    }

    [HttpGet("{tenantId:guid}/cases")]
    [Authorize(Policy = "AdminSupport")]
    [ProducesResponseType(typeof(ApiEnvelope<TenantCasesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantCases(Guid tenantId, CancellationToken cancellationToken)
    {
        var result = await _tenantService.GetTenantCasesAsync(tenantId, cancellationToken);
        if (result is null)
            return NotFound(new ProblemDetails { Title = "Tenant not found", Status = 404 });

        return Ok(ApiEnvelope<TenantCasesResponse>.Success(result, BuildMeta()));
    }

    [HttpGet("{tenantId:guid}/activity")]
    [Authorize(Policy = "AdminSupport")]
    [ProducesResponseType(typeof(ApiEnvelope<TenantActivityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantActivity(Guid tenantId, CancellationToken cancellationToken)
    {
        var result = await _tenantService.GetTenantActivityAsync(tenantId, cancellationToken);
        if (result is null)
            return NotFound(new ProblemDetails { Title = "Tenant not found", Status = 404 });

        return Ok(ApiEnvelope<TenantActivityResponse>.Success(result, BuildMeta()));
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

    private static Dictionary<string, object?> BuildMeta()
        => new() { ["timestamp"] = DateTime.UtcNow };
}
