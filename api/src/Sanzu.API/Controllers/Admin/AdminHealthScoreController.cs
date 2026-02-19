using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;

namespace Sanzu.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/health-scores")]
[Authorize(Policy = "AdminOps")]
public sealed class AdminHealthScoreController : ControllerBase
{
    private readonly IHealthScoreComputeService _healthScoreComputeService;

    public AdminHealthScoreController(IHealthScoreComputeService healthScoreComputeService)
    {
        _healthScoreComputeService = healthScoreComputeService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<TenantHealthScoreResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var scores = await _healthScoreComputeService.GetLatestScoresAsync(cancellationToken);

        var meta = new Dictionary<string, object?>
        {
            ["timestamp"] = DateTime.UtcNow
        };

        return Ok(ApiEnvelope<IReadOnlyList<TenantHealthScoreResponse>>.Success(scores, meta));
    }

    [HttpPost("compute")]
    [Authorize(Policy = "AdminFull")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Compute(CancellationToken cancellationToken)
    {
        await _healthScoreComputeService.ComputeForAllTenantsAsync(cancellationToken);
        return NoContent();
    }
}
