using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Tests.Integration.Controllers;

public sealed class RemediationControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RemediationControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Preview_ShouldReturnImpact_WhenActionTypeValid()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "remediation-preview@agency.pt");
        await GrantSanzuAdminAsync(signup.UserId);

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/admin/remediation/preview?actionType=contact_tenant&tenantId={signup.OrganizationId}",
            signup.UserId, Guid.Empty, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<RemediationImpactPreviewResponse>>();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.ActionType.Should().Be("contact_tenant");
        envelope.Data.IsReversible.Should().BeTrue();
    }

    [Fact]
    public async Task Commit_ShouldRequireAuditNote()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "remediation-nonote@agency.pt");
        await GrantSanzuAdminAsync(signup.UserId);

        using var request = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            "/api/v1/admin/remediation/commit",
            new CommitRemediationRequest
            {
                QueueId = "ADM_FailedPayment",
                QueueItemId = $"payment-{signup.OrganizationId}",
                TenantId = signup.OrganizationId,
                ActionType = "contact_tenant",
                AuditNote = ""
            },
            signup.UserId, Guid.Empty, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task FullRemediationFlow_CommitVerifyResolve()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "remediation-flow@agency.pt");
        await GrantSanzuAdminAsync(signup.UserId);

        // Commit
        using var commitRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            "/api/v1/admin/remediation/commit",
            new CommitRemediationRequest
            {
                QueueId = "ADM_FailedPayment",
                QueueItemId = $"payment-{signup.OrganizationId}",
                TenantId = signup.OrganizationId,
                ActionType = "extend_grace_period",
                AuditNote = "Extending grace period due to bank transfer delay."
            },
            signup.UserId, Guid.Empty, "SanzuAdmin");

        var commitResponse = await client.SendAsync(commitRequest);
        commitResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var commitEnvelope = await commitResponse.Content.ReadFromJsonAsync<ApiEnvelope<RemediationActionResponse>>();
        commitEnvelope!.Data.Should().NotBeNull();
        var remediationId = commitEnvelope.Data!.Id;
        commitEnvelope.Data.Status.Should().Be("Committed");

        // Verify
        using var verifyRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/admin/remediation/{remediationId}/verify",
            new VerifyRemediationRequest
            {
                VerificationType = "manual",
                VerificationResult = "Payment received",
                Passed = true
            },
            signup.UserId, Guid.Empty, "SanzuAdmin");

        var verifyResponse = await client.SendAsync(verifyRequest);
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var verifyEnvelope = await verifyResponse.Content.ReadFromJsonAsync<ApiEnvelope<RemediationActionResponse>>();
        verifyEnvelope!.Data!.Status.Should().Be("Verified");

        // Resolve
        using var resolveRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/admin/remediation/{remediationId}/resolve",
            new ResolveRemediationRequest(),
            signup.UserId, Guid.Empty, "SanzuAdmin");

        var resolveResponse = await client.SendAsync(resolveRequest);
        resolveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var resolveEnvelope = await resolveResponse.Content.ReadFromJsonAsync<ApiEnvelope<RemediationActionResponse>>();
        resolveEnvelope!.Data!.Status.Should().Be("Resolved");
        resolveEnvelope.Data.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Resolve_ShouldRequireOverrideNote_WhenVerificationFailed()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "remediation-override@agency.pt");
        await GrantSanzuAdminAsync(signup.UserId);

        // Commit
        using var commitRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            "/api/v1/admin/remediation/commit",
            new CommitRemediationRequest
            {
                QueueId = "ADM_SupportEscalation",
                QueueItemId = $"escalation-{signup.OrganizationId}",
                TenantId = signup.OrganizationId,
                ActionType = "escalate_to_support",
                AuditNote = "Escalating due to repeated system errors."
            },
            signup.UserId, Guid.Empty, "SanzuAdmin");

        var commitResponse = await client.SendAsync(commitRequest);
        commitResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var commitEnvelope = await commitResponse.Content.ReadFromJsonAsync<ApiEnvelope<RemediationActionResponse>>();
        var remediationId = commitEnvelope!.Data!.Id;

        // Verify (failed)
        using var verifyRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/admin/remediation/{remediationId}/verify",
            new VerifyRemediationRequest
            {
                VerificationType = "automatic",
                VerificationResult = "Errors persist",
                Passed = false
            },
            signup.UserId, Guid.Empty, "SanzuAdmin");

        var verifyResponse = await client.SendAsync(verifyRequest);
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var verifyEnvelope = await verifyResponse.Content.ReadFromJsonAsync<ApiEnvelope<RemediationActionResponse>>();
        verifyEnvelope!.Data!.Status.Should().Be("VerificationFailed");

        // Resolve without override should fail
        using var resolveNoNote = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/admin/remediation/{remediationId}/resolve",
            new ResolveRemediationRequest(),
            signup.UserId, Guid.Empty, "SanzuAdmin");

        var resolveFailResponse = await client.SendAsync(resolveNoNote);
        resolveFailResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Resolve with override note should succeed
        using var resolveWithNote = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/admin/remediation/{remediationId}/resolve",
            new ResolveRemediationRequest { OverrideNote = "Accepted risk; monitoring enabled." },
            signup.UserId, Guid.Empty, "SanzuAdmin");

        var resolveOkResponse = await client.SendAsync(resolveWithNote);
        resolveOkResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var resolveEnvelope = await resolveOkResponse.Content.ReadFromJsonAsync<ApiEnvelope<RemediationActionResponse>>();
        resolveEnvelope!.Data!.Status.Should().Be("Resolved");
    }

    private async Task GrantSanzuAdminAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = null,
            RoleType = PlatformRole.SanzuAdmin,
            GrantedAt = DateTime.UtcNow,
            GrantedBy = userId
        });
        await dbContext.SaveChangesAsync();
    }

    private static HttpRequestMessage BuildAuthorizedJsonRequest(
        HttpMethod method, string uri, object payload, Guid userId, Guid tenantId, string role)
    {
        var message = new HttpRequestMessage(method, uri) { Content = JsonContent.Create(payload) };
        message.Headers.Add("X-User-Id", userId.ToString());
        message.Headers.Add("X-Tenant-Id", tenantId.ToString());
        message.Headers.Add("X-User-Role", role);
        return message;
    }

    private static HttpRequestMessage BuildAuthorizedRequest(
        HttpMethod method, string uri, Guid userId, Guid tenantId, string role)
    {
        var message = new HttpRequestMessage(method, uri);
        message.Headers.Add("X-User-Id", userId.ToString());
        message.Headers.Add("X-Tenant-Id", tenantId.ToString());
        message.Headers.Add("X-User-Role", role);
        return message;
    }

    private static async Task<CreateAgencyAccountResponse> CreateTenantAsync(HttpClient client, string email)
    {
        var request = new CreateAgencyAccountRequest
        {
            Email = email, FullName = "Agency Admin", AgencyName = "Agency", Location = "Lisbon"
        };
        var signupResponse = await client.PostAsJsonAsync("/api/v1/tenants/signup", request);
        signupResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var envelope = await signupResponse.Content.ReadFromJsonAsync<ApiEnvelope<CreateAgencyAccountResponse>>();
        return envelope!.Data!;
    }
}
