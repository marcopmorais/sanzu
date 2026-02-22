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
