using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;

namespace Sanzu.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/platform")]
[Authorize(Policy = "AdminFull")]
public sealed class AdminPlatformController : ControllerBase
{
    private readonly IPlatformSummaryService _platformSummaryService;

    public AdminPlatformController(IPlatformSummaryService platformSummaryService)
    {
        _platformSummaryService = platformSummaryService;
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiEnvelope<PlatformOperationsSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await _platformSummaryService.GetSummaryAsync(cancellationToken);

        return Ok(ApiEnvelope<PlatformOperationsSummaryResponse>.Success(
            summary,
            new Dictionary<string, object?>
            {
                ["timestamp"] = DateTime.UtcNow
            }));
    }
}
