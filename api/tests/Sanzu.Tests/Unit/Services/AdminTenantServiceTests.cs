using FluentAssertions;
using Moq;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Services;

namespace Sanzu.Tests.Unit.Services;

public sealed class AdminTenantServiceTests
{
    private readonly Mock<IOrganizationRepository> _orgRepoMock = new();
    private readonly Mock<ITenantHealthScoreRepository> _healthRepoMock = new();
    private readonly Mock<IBillingRecordRepository> _billingRepoMock = new();
    private readonly Mock<ICaseRepository> _caseRepoMock = new();
    private readonly Mock<IAuditRepository> _auditRepoMock = new();

    private AdminTenantService CreateService()
        => new(
            _orgRepoMock.Object,
            _healthRepoMock.Object,
            _billingRepoMock.Object,
            _caseRepoMock.Object,
            _auditRepoMock.Object);

    // ═══════════════════════════════════════════════════════════
    // Story 15.1 — ListTenants tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task ListTenants_Should_MapFieldsCorrectly()
    {
        var tenantId = Guid.NewGuid();
        var org = new Organization
        {
            Id = tenantId,
            Name = "Test Agency",
            Status = TenantStatus.Active,
            SubscriptionPlan = "Profissional",
            Location = "EU-West",
            CreatedAt = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc)
        };

        var healthScore = new TenantHealthScore
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OverallScore = 72,
            BillingScore = 80,
            CaseCompletionScore = 60,
            OnboardingScore = 75,
            HealthBand = HealthBand.Green,
            ComputedAt = DateTime.UtcNow
        };

        _orgRepoMock
            .Setup(r => r.SearchForPlatformAsync(It.IsAny<TenantListRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Organization> { org } as IReadOnlyList<Organization>, 1));

        _healthRepoMock
            .Setup(r => r.GetLatestForAllTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TenantHealthScore> { healthScore });

        var service = CreateService();
        var result = await service.ListTenantsAsync(new TenantListRequest(), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.Id.Should().Be(tenantId);
        item.Name.Should().Be("Test Agency");
        item.Status.Should().Be("Active");
        item.PlanTier.Should().Be("Profissional");
        item.HealthScore.Should().Be(72);
        item.HealthBand.Should().Be("Green");
        item.SignupDate.Should().Be(new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc));
        item.Region.Should().Be("EU-West");
    }

    [Fact]
    public async Task ListTenants_Should_ReturnNullHealthFields_WhenNoScoreExists()
    {
        var tenantId = Guid.NewGuid();
        var org = new Organization
        {
            Id = tenantId,
            Name = "No Score Agency",
            Status = TenantStatus.Onboarding,
            Location = "",
            CreatedAt = DateTime.UtcNow
        };

        _orgRepoMock
            .Setup(r => r.SearchForPlatformAsync(It.IsAny<TenantListRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Organization> { org } as IReadOnlyList<Organization>, 1));

        _healthRepoMock
            .Setup(r => r.GetLatestForAllTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TenantHealthScore>());

        var service = CreateService();
        var result = await service.ListTenantsAsync(new TenantListRequest(), CancellationToken.None);

        var item = result.Items[0];
        item.HealthScore.Should().BeNull();
        item.HealthBand.Should().BeNull();
        item.Region.Should().BeNull("empty Location maps to null region");
    }

    [Fact]
    public async Task ListTenants_Should_PlaceNullScoreTenantsLast_WhenSortByHealthScoreAsc()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var tenantNoScore = Guid.NewGuid();

        var orgs = new List<Organization>
        {
            new() { Id = tenantA, Name = "A", Status = TenantStatus.Active, CreatedAt = DateTime.UtcNow },
            new() { Id = tenantB, Name = "B", Status = TenantStatus.Active, CreatedAt = DateTime.UtcNow },
            new() { Id = tenantNoScore, Name = "NoScore", Status = TenantStatus.Active, CreatedAt = DateTime.UtcNow }
        };

        var scores = new List<TenantHealthScore>
        {
            new() { Id = Guid.NewGuid(), TenantId = tenantA, OverallScore = 80, HealthBand = HealthBand.Green, ComputedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), TenantId = tenantB, OverallScore = 30, HealthBand = HealthBand.Red, ComputedAt = DateTime.UtcNow }
        };

        _orgRepoMock
            .Setup(r => r.SearchForPlatformAsync(It.IsAny<TenantListRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((orgs as IReadOnlyList<Organization>, orgs.Count));

        _healthRepoMock
            .Setup(r => r.GetLatestForAllTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(scores);

        var service = CreateService();
        var result = await service.ListTenantsAsync(
            new TenantListRequest { Sort = "healthScore", Order = "asc" },
            CancellationToken.None);

        result.Items.Should().HaveCount(3);
        result.Items[0].Name.Should().Be("B");
        result.Items[1].Name.Should().Be("A");
        result.Items[2].Name.Should().Be("NoScore");
        result.Items[2].HealthScore.Should().BeNull();
    }

    // ═══════════════════════════════════════════════════════════
    // Story 15.2 — Tenant 360 unit tests
    // ═══════════════════════════════════════════════════════════

    // ── GetTenantSummaryAsync ──

    [Fact]
    public async Task GetTenantSummary_Should_MapFieldsCorrectly()
    {
        var tenantId = Guid.NewGuid();
        var org = new Organization
        {
            Id = tenantId,
            Name = "Summary Agency",
            Status = TenantStatus.Active,
            SubscriptionPlan = "Enterprise",
            Location = "US-East",
            InvoiceProfileBillingEmail = "billing@agency.com",
            CreatedAt = new DateTime(2025, 3, 10, 0, 0, 0, DateTimeKind.Utc)
        };

        var healthScore = new TenantHealthScore
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OverallScore = 85,
            HealthBand = HealthBand.Green,
            ComputedAt = DateTime.UtcNow
        };

        _orgRepoMock
            .Setup(r => r.GetByIdForPlatformAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);
        _healthRepoMock
            .Setup(r => r.GetLatestByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthScore);

        var service = CreateService();
        var result = await service.GetTenantSummaryAsync(tenantId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(tenantId);
        result.Name.Should().Be("Summary Agency");
        result.Status.Should().Be("Active");
        result.PlanTier.Should().Be("Enterprise");
        result.Region.Should().Be("US-East");
        result.ContactEmail.Should().Be("billing@agency.com");
        result.HealthScore.Should().Be(85);
        result.HealthBand.Should().Be("Green");
        result.SignupDate.Should().Be(new DateTime(2025, 3, 10, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GetTenantSummary_Should_ReturnNull_WhenTenantNotFound()
    {
        _orgRepoMock
            .Setup(r => r.GetByIdForPlatformAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Organization?)null);

        var service = CreateService();
        var result = await service.GetTenantSummaryAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    // ── GetTenantBillingAsync ──

    [Fact]
    public async Task GetTenantBilling_Should_DerivePaidHealth_WhenNoFailedPayments()
    {
        var tenantId = Guid.NewGuid();
        var org = new Organization
        {
            Id = tenantId,
            Name = "Paid Agency",
            Status = TenantStatus.Active,
            SubscriptionPlan = "Profissional",
            SubscriptionBillingCycle = "Monthly",
            SubscriptionActivatedAt = DateTime.UtcNow.AddMonths(-6),
            FailedPaymentAttempts = 0,
            CreatedAt = DateTime.UtcNow
        };

        _orgRepoMock
            .Setup(r => r.GetByIdForPlatformAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);
        _billingRepoMock
            .Setup(r => r.GetByTenantIdForPlatformAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BillingRecord>());

        var service = CreateService();
        var result = await service.GetTenantBillingAsync(tenantId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.BillingHealth.Should().Be("Paid");
        result.GracePeriodActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetTenantBilling_Should_DeriveFailedHealth_WhenPaymentsFailed()
    {
        var tenantId = Guid.NewGuid();
        var org = new Organization
        {
            Id = tenantId,
            Name = "Failed Agency",
            Status = TenantStatus.PaymentIssue,
            FailedPaymentAttempts = 3,
            LastPaymentFailedAt = DateTime.UtcNow.AddDays(-5),
            NextPaymentRetryAt = null,
            CreatedAt = DateTime.UtcNow
        };

        _orgRepoMock
            .Setup(r => r.GetByIdForPlatformAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);
        _billingRepoMock
            .Setup(r => r.GetByTenantIdForPlatformAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BillingRecord>());

        var service = CreateService();
        var result = await service.GetTenantBillingAsync(tenantId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.BillingHealth.Should().Be("Failed");
    }

    [Fact]
    public async Task GetTenantBilling_Should_LimitInvoicesToFive()
    {
        var tenantId = Guid.NewGuid();
        var org = new Organization
        {
            Id = tenantId,
            Name = "Many Invoices",
            Status = TenantStatus.Active,
            FailedPaymentAttempts = 0,
            CreatedAt = DateTime.UtcNow
        };

        var records = Enumerable.Range(1, 10).Select(i => new BillingRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceNumber = $"INV-{i:D5}",
            BillingCycleStart = DateTime.UtcNow.AddMonths(-i),
            BillingCycleEnd = DateTime.UtcNow.AddMonths(-i + 1),
            TotalAmount = 100m * i,
            Currency = "EUR",
            Status = "FINALIZED",
            CreatedAt = DateTime.UtcNow.AddMonths(-i)
        }).ToList();

        _orgRepoMock
            .Setup(r => r.GetByIdForPlatformAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);
        _billingRepoMock
            .Setup(r => r.GetByTenantIdForPlatformAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);

        var service = CreateService();
        var result = await service.GetTenantBillingAsync(tenantId, CancellationToken.None);

        result!.RecentInvoices.Should().HaveCount(5);
    }

    // ── GetTenantCasesAsync ──

    [Fact]
    public async Task GetTenantCases_Should_ComputeWorkflowProgressCorrectly()
    {
        var tenantId = Guid.NewGuid();
        var caseId = Guid.NewGuid();

        var org = new Organization { Id = tenantId, Name = "Cases Agency", Status = TenantStatus.Active, CreatedAt = DateTime.UtcNow };
        var caseEntity = new Case
        {
            Id = caseId,
            TenantId = tenantId,
            CaseNumber = "CASE-00001",
            DeceasedFullName = "John Doe",
            Status = CaseStatus.Active,
            WorkflowKey = "standard",
            ManagerUserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WorkflowSteps = new List<WorkflowStepInstance>
            {
                new() { Id = Guid.NewGuid(), CaseId = caseId, TenantId = tenantId, StepKey = "step-1", Title = "Step 1", Sequence = 1, Status = WorkflowStepStatus.Complete, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), CaseId = caseId, TenantId = tenantId, StepKey = "step-2", Title = "Step 2", Sequence = 2, Status = WorkflowStepStatus.InProgress, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), CaseId = caseId, TenantId = tenantId, StepKey = "step-3", Title = "Step 3", Sequence = 3, Status = WorkflowStepStatus.Blocked, BlockedReasonCode = BlockedReasonCode.ExternalDependency, BlockedReasonDetail = "Waiting on step 2", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), CaseId = caseId, TenantId = tenantId, StepKey = "step-4", Title = "Step 4", Sequence = 4, Status = WorkflowStepStatus.NotStarted, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            }
        };

        _orgRepoMock
            .Setup(r => r.GetByIdForPlatformAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);
        _caseRepoMock
            .Setup(r => r.GetByTenantIdWithStepsForPlatformAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Case> { caseEntity });

        var service = CreateService();
        var result = await service.GetTenantCasesAsync(tenantId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Cases.Should().HaveCount(1);

        var caseItem = result.Cases[0];
        caseItem.CaseNumber.Should().Be("CASE-00001");
        caseItem.WorkflowProgress.TotalSteps.Should().Be(4);
        caseItem.WorkflowProgress.CompletedSteps.Should().Be(1);
        caseItem.WorkflowProgress.InProgressSteps.Should().Be(1);
        caseItem.WorkflowProgress.BlockedSteps.Should().Be(1);
    }

    [Fact]
    public async Task GetTenantCases_Should_ExtractBlockedStepDetails()
    {
        var tenantId = Guid.NewGuid();
        var caseId = Guid.NewGuid();

        var org = new Organization { Id = tenantId, Name = "Blocked Agency", Status = TenantStatus.Active, CreatedAt = DateTime.UtcNow };
        var caseEntity = new Case
        {
            Id = caseId,
            TenantId = tenantId,
            CaseNumber = "CASE-00002",
            DeceasedFullName = "Jane Doe",
            Status = CaseStatus.Active,
            ManagerUserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WorkflowSteps = new List<WorkflowStepInstance>
            {
                new() { Id = Guid.NewGuid(), CaseId = caseId, TenantId = tenantId, StepKey = "doc-upload", Title = "Document Upload", Sequence = 1, Status = WorkflowStepStatus.Blocked, BlockedReasonCode = BlockedReasonCode.EvidenceMissing, BlockedReasonDetail = "Birth certificate required", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            }
        };

        _orgRepoMock
            .Setup(r => r.GetByIdForPlatformAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);
        _caseRepoMock
            .Setup(r => r.GetByTenantIdWithStepsForPlatformAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Case> { caseEntity });

        var service = CreateService();
        var result = await service.GetTenantCasesAsync(tenantId, CancellationToken.None);

        result!.Cases[0].BlockedSteps.Should().HaveCount(1);
        var blocked = result.Cases[0].BlockedSteps[0];
        blocked.StepKey.Should().Be("doc-upload");
        blocked.Title.Should().Be("Document Upload");
        blocked.BlockedReasonCode.Should().Be("EvidenceMissing");
        blocked.BlockedReasonDetail.Should().Be("Birth certificate required");
    }

    // ── GetTenantActivityAsync ──

    [Fact]
    public async Task GetTenantActivity_Should_ReturnEventsWithin30DayWindow()
    {
        var tenantId = Guid.NewGuid();
        var caseId = Guid.NewGuid();

        var org = new Organization { Id = tenantId, Name = "Activity Agency", Status = TenantStatus.Active, CreatedAt = DateTime.UtcNow };

        var events = new List<AuditEvent>
        {
            new() { Id = Guid.NewGuid(), CaseId = caseId, ActorUserId = Guid.NewGuid(), EventType = "Case.Created", Metadata = "{}", CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new() { Id = Guid.NewGuid(), CaseId = caseId, ActorUserId = Guid.NewGuid(), EventType = "Document.Uploaded", Metadata = "{}", CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };

        _orgRepoMock
            .Setup(r => r.GetByIdForPlatformAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);
        _auditRepoMock
            .Setup(r => r.GetByTenantIdInPeriodAsync(tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        var service = CreateService();
        var result = await service.GetTenantActivityAsync(tenantId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Events.Should().HaveCount(2);
        // Most recent first
        result.Events[0].EventType.Should().Be("Document.Uploaded");
        result.Events[1].EventType.Should().Be("Case.Created");
    }
}
