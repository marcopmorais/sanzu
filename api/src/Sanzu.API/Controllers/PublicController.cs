using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.API.Controllers;

[ApiController]
[Route("api/v1/public")]
public sealed class PublicController : ControllerBase
{
    private readonly IPublicConversionService _publicConversionService;

    public PublicController(IPublicConversionService publicConversionService)
    {
        _publicConversionService = publicConversionService;
    }

    [HttpPost("demo-request")]
    [ProducesResponseType(typeof(ApiEnvelope<PublicLeadCaptureResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitDemoRequest(
        [FromBody] SubmitDemoRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _publicConversionService.SubmitDemoRequestAsync(
                request,
                Request.Headers.UserAgent.ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);

            return Created(
                $"/api/v1/public/leads/{response.LeadId}",
                ApiEnvelope<PublicLeadCaptureResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid demo request payload"));
        }
    }

    [HttpPost("start-account")]
    [ProducesResponseType(typeof(ApiEnvelope<PublicLeadCaptureResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartAccountIntent(
        [FromBody] StartAccountIntentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _publicConversionService.StartAccountIntentAsync(
                request,
                Request.Headers.UserAgent.ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);

            return Created(
                $"/api/v1/public/leads/{response.LeadId}",
                ApiEnvelope<PublicLeadCaptureResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid start account payload"));
        }
    }

    private static ValidationProblemDetails BuildValidationProblem(ValidationException validationException, string title)
    {
        var errors = validationException.Errors
            .GroupBy(
                x => string.IsNullOrWhiteSpace(x.PropertyName)
                    ? "request"
                    : x.PropertyName)
            .ToDictionary(
                x => x.Key,
                x => x.Select(e => e.ErrorMessage).Distinct().ToArray());

        return new ValidationProblemDetails(errors)
        {
            Title = title,
            Status = StatusCodes.Status400BadRequest
        };
    }

    private static Dictionary<string, object?> BuildMeta()
    {
        return new Dictionary<string, object?>
        {
            ["timestamp"] = DateTime.UtcNow
        };
    }
}
