using FluentValidation;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.API.Controllers;

[ApiController]
[Route("api/v1/tenants")]
public sealed class TenantsController : ControllerBase
{
    private readonly ITenantOnboardingService _tenantOnboardingService;
    private readonly ITenantSubscriptionService _tenantSubscriptionService;
    private readonly ITenantBillingService _tenantBillingService;

    public TenantsController(
        ITenantOnboardingService tenantOnboardingService,
        ITenantSubscriptionService tenantSubscriptionService,
        ITenantBillingService tenantBillingService)
    {
        _tenantOnboardingService = tenantOnboardingService;
        _tenantSubscriptionService = tenantSubscriptionService;
        _tenantBillingService = tenantBillingService;
    }

    [HttpPost("signup")]
    [ProducesResponseType(typeof(ApiEnvelope<CreateAgencyAccountResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Signup(
        [FromBody] CreateAgencyAccountRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _tenantOnboardingService.CreateAgencyAccountAsync(request, cancellationToken);
            var meta = new Dictionary<string, object?>
            {
                ["timestamp"] = DateTime.UtcNow
            };

            return Created(
                $"/api/v1/tenants/{response.OrganizationId}",
                ApiEnvelope<CreateAgencyAccountResponse>.Success(response, meta));
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
                Title = "Invalid signup request",
                Status = StatusCodes.Status400BadRequest
            };

            return BadRequest(details);
        }
        catch (DuplicateEmailException)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Unable to create account",
                detail: "The account could not be created with the provided information.");
        }
    }

    [Authorize(Policy = "TenantAdmin")]
    [HttpPatch("{tenantId:guid}/onboarding/profile")]
    [ProducesResponseType(typeof(ApiEnvelope<TenantOnboardingProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateOnboardingProfile(
        Guid tenantId,
        [FromBody] UpdateTenantOnboardingProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _tenantOnboardingService.UpdateOnboardingProfileAsync(
                tenantId,
                actorUserId,
                request,
                cancellationToken);

            return Ok(ApiEnvelope<TenantOnboardingProfileResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid onboarding profile request"));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (TenantOnboardingStateException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Onboarding state conflict",
                detail: exception.Message);
        }
    }

    [Authorize(Policy = "TenantAdmin")]
    [HttpPatch("{tenantId:guid}/onboarding/defaults")]
    [ProducesResponseType(typeof(ApiEnvelope<TenantOnboardingDefaultsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateOnboardingDefaults(
        Guid tenantId,
        [FromBody] UpdateTenantOnboardingDefaultsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _tenantOnboardingService.UpdateOnboardingDefaultsAsync(
                tenantId,
                actorUserId,
                request,
                cancellationToken);

            return Ok(ApiEnvelope<TenantOnboardingDefaultsResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid onboarding defaults request"));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (TenantOnboardingStateException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Onboarding state conflict",
                detail: exception.Message);
        }
    }

    [Authorize(Policy = "TenantAdmin")]
    [HttpPost("{tenantId:guid}/onboarding/invitations")]
    [ProducesResponseType(typeof(ApiEnvelope<TenantInvitationResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateOnboardingInvitation(
        Guid tenantId,
        [FromBody] CreateTenantInvitationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _tenantOnboardingService.CreateInvitationAsync(
                tenantId,
                actorUserId,
                request,
                cancellationToken);

            return Created(
                $"/api/v1/tenants/{tenantId}/onboarding/invitations/{response.InvitationId}",
                ApiEnvelope<TenantInvitationResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid tenant invitation request"));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (TenantOnboardingConflictException)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Unable to create invitation",
                detail: "The invitation could not be created with the provided information.");
        }
        catch (TenantOnboardingStateException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Onboarding state conflict",
                detail: exception.Message);
        }
    }

    [Authorize(Policy = "TenantAdmin")]
    [HttpPost("{tenantId:guid}/onboarding/complete")]
    [ProducesResponseType(typeof(ApiEnvelope<TenantOnboardingCompletionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CompleteOnboarding(
        Guid tenantId,
        [FromBody] CompleteTenantOnboardingRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _tenantOnboardingService.CompleteOnboardingAsync(
                tenantId,
                actorUserId,
                request,
                cancellationToken);

            return Ok(ApiEnvelope<TenantOnboardingCompletionResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid onboarding completion request"));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (TenantOnboardingStateException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Onboarding state conflict",
                detail: exception.Message);
        }
    }

    [Authorize(Policy = "TenantAdmin")]
    [HttpPost("{tenantId:guid}/onboarding/billing/activate")]
    [ProducesResponseType(typeof(ApiEnvelope<TenantBillingActivationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ActivateBilling(
        Guid tenantId,
        [FromBody] ActivateTenantBillingRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _tenantOnboardingService.ActivateBillingAsync(
                tenantId,
                actorUserId,
                request,
                cancellationToken);

            return Ok(ApiEnvelope<TenantBillingActivationResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid billing activation request"));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (TenantOnboardingStateException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Onboarding state conflict",
                detail: exception.Message);
        }
    }

    [Authorize(Policy = "TenantAdmin")]
    [HttpPost("{tenantId:guid}/subscription/preview-change")]
    [ProducesResponseType(typeof(ApiEnvelope<PlanChangePreviewResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PreviewPlanChange(
        Guid tenantId,
        [FromBody] PreviewPlanChangeRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _tenantSubscriptionService.PreviewPlanChangeAsync(
                tenantId,
                actorUserId,
                request,
                cancellationToken);

            return Ok(ApiEnvelope<PlanChangePreviewResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid plan change preview request"));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (TenantOnboardingStateException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Subscription state conflict",
                detail: exception.Message);
        }
    }

    [Authorize(Policy = "TenantAdmin")]
    [HttpPatch("{tenantId:guid}/subscription/plan")]
    [ProducesResponseType(typeof(ApiEnvelope<ChangePlanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangePlan(
        Guid tenantId,
        [FromBody] ChangePlanRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _tenantSubscriptionService.ChangePlanAsync(
                tenantId,
                actorUserId,
                request,
                cancellationToken);

            return Ok(ApiEnvelope<ChangePlanResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid plan change request"));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (TenantOnboardingConflictException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Plan change conflict",
                detail: exception.Message);
        }
        catch (TenantOnboardingStateException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Subscription state conflict",
                detail: exception.Message);
        }
    }

    [Authorize(Policy = "TenantAdmin")]
    [HttpPost("{tenantId:guid}/subscription/cancel")]
    [ProducesResponseType(typeof(ApiEnvelope<CancelSubscriptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelSubscription(
        Guid tenantId,
        [FromBody] CancelSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _tenantSubscriptionService.CancelSubscriptionAsync(
                tenantId,
                actorUserId,
                request,
                cancellationToken);

            return Ok(ApiEnvelope<CancelSubscriptionResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid cancellation request"));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (TenantOnboardingStateException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Subscription state conflict",
                detail: exception.Message);
        }
    }

    [Authorize(Policy = "TenantAdmin")]
    [HttpPost("{tenantId:guid}/billing/invoices/generate")]
    [ProducesResponseType(typeof(ApiEnvelope<BillingRecordResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GenerateInvoice(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _tenantBillingService.CreateBillingRecordAsync(
                tenantId,
                actorUserId,
                cancellationToken);

            return Created(
                $"/api/v1/tenants/{tenantId}/billing/invoices/{response.Id}",
                ApiEnvelope<BillingRecordResponse>.Success(response, BuildMeta()));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (TenantOnboardingStateException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Billing state conflict",
                detail: exception.Message);
        }
    }

    [Authorize(Policy = "TenantAdmin")]
    [HttpGet("{tenantId:guid}/billing/history")]
    [ProducesResponseType(typeof(ApiEnvelope<BillingHistoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetBillingHistory(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _tenantBillingService.GetBillingHistoryAsync(
                tenantId,
                actorUserId,
                cancellationToken);

            return Ok(ApiEnvelope<BillingHistoryResponse>.Success(response, BuildMeta()));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (TenantOnboardingStateException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Billing state conflict",
                detail: exception.Message);
        }
    }

    [Authorize(Policy = "TenantAdmin")]
    [HttpGet("{tenantId:guid}/billing/usage")]
    [ProducesResponseType(typeof(ApiEnvelope<BillingUsageSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetBillingUsage(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _tenantBillingService.GetUsageSummaryAsync(
                tenantId,
                actorUserId,
                cancellationToken);

            return Ok(ApiEnvelope<BillingUsageSummaryResponse>.Success(response, BuildMeta()));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (TenantOnboardingStateException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Billing state conflict",
                detail: exception.Message);
        }
    }

    [Authorize(Policy = "TenantAdmin")]
    [HttpGet("{tenantId:guid}/billing/invoices/{invoiceId:guid}")]
    [ProducesResponseType(typeof(ApiEnvelope<InvoiceDownloadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetInvoice(
        Guid tenantId,
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _tenantBillingService.GetInvoiceAsync(
                tenantId,
                actorUserId,
                invoiceId,
                cancellationToken);

            return Ok(ApiEnvelope<InvoiceDownloadResponse>.Success(response, BuildMeta()));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (TenantOnboardingStateException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Billing state conflict",
                detail: exception.Message);
        }
    }

    [Authorize(Policy = "TenantAdmin")]
    [HttpPost("{tenantId:guid}/billing/payment-failures")]
    [ProducesResponseType(typeof(ApiEnvelope<PaymentRecoveryStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterFailedPayment(
        Guid tenantId,
        [FromBody] RegisterFailedPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _tenantBillingService.RegisterFailedPaymentAsync(
                tenantId,
                actorUserId,
                request,
                cancellationToken);

            return Ok(ApiEnvelope<PaymentRecoveryStatusResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid failed payment request"));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (TenantOnboardingStateException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Billing state conflict",
                detail: exception.Message);
        }
    }

    [Authorize(Policy = "TenantAdmin")]
    [HttpPost("{tenantId:guid}/billing/recovery/execute")]
    [ProducesResponseType(typeof(ApiEnvelope<PaymentRecoveryStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ExecutePaymentRecovery(
        Guid tenantId,
        [FromBody] ExecutePaymentRecoveryRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _tenantBillingService.ExecutePaymentRecoveryAsync(
                tenantId,
                actorUserId,
                request,
                cancellationToken);

            return Ok(ApiEnvelope<PaymentRecoveryStatusResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid payment recovery request"));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (TenantOnboardingStateException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Billing state conflict",
                detail: exception.Message);
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
