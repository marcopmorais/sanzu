using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Services;
using Sanzu.Core.Validators;
using Sanzu.Infrastructure.Data;
using Sanzu.Infrastructure.Repositories;

namespace Sanzu.Tests.Unit.Services;

public sealed class TenantBillingServiceTests
{
    [Fact]
    public async Task CreateBillingRecord_ShouldPersistRecordAndAudit_WhenTenantIsActive()
    {
        var dbContext = CreateContext();
        var (tenantId, userId) = await SeedTenantAsync(dbContext, TenantStatus.Active, "GROWTH", "MONTHLY");
        var service = CreateService(dbContext);

        var result = await service.CreateBillingRecordAsync(
            tenantId,
            userId,
            CancellationToken.None);

        result.InvoiceNumber.Should().StartWith("INV-");
        result.PlanCode.Should().Be("GROWTH");
        result.BillingCycle.Should().Be("MONTHLY");
        result.Currency.Should().Be("EUR");
        result.Status.Should().Be("FINALIZED");

        var persisted = await dbContext.BillingRecords.SingleAsync(x => x.Id == result.Id);
        persisted.TotalAmount.Should().Be(result.TotalAmount);
        persisted.InvoiceSnapshot.Should().Contain(result.InvoiceNumber);
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "TenantBillingRecordCreated");
    }

    [Fact]
    public async Task GetBillingHistory_ShouldReturnRecords_WhenTenantIsSuspended()
    {
        var dbContext = CreateContext();
        var (tenantId, userId) = await SeedTenantAsync(dbContext, TenantStatus.Suspended, "STARTER", "MONTHLY");
        await SeedBillingRecordAsync(dbContext, tenantId, "INV-00001", "STARTER", "MONTHLY");
        var service = CreateService(dbContext);

        var result = await service.GetBillingHistoryAsync(
            tenantId,
            userId,
            CancellationToken.None);

        result.TenantId.Should().Be(tenantId);
        result.CurrentPlan.Should().Be("STARTER");
        result.CurrentBillingCycle.Should().Be("MONTHLY");
        result.Records.Should().HaveCount(1);
        result.Records[0].InvoiceNumber.Should().Be("INV-00001");
    }

    [Fact]
    public async Task GetBillingHistory_ShouldThrowStateException_WhenTenantIsPending()
    {
        var dbContext = CreateContext();
        var (tenantId, userId) = await SeedTenantAsync(dbContext, TenantStatus.Pending, "STARTER", "MONTHLY");
        var service = CreateService(dbContext);

        var act = () => service.GetBillingHistoryAsync(
            tenantId,
            userId,
            CancellationToken.None);

        await act.Should().ThrowAsync<TenantOnboardingStateException>()
            .WithMessage("*active, payment-issue, or suspended*");
    }

    [Fact]
    public async Task GetUsageSummary_ShouldReturnCurrentPlanUsage_WhenTenantIsActive()
    {
        var dbContext = CreateContext();
        var (tenantId, userId) = await SeedTenantAsync(dbContext, TenantStatus.Active, "STARTER", "MONTHLY");
        var service = CreateService(dbContext);

        var result = await service.GetUsageSummaryAsync(
            tenantId,
            userId,
            CancellationToken.None);

        result.TenantId.Should().Be(tenantId);
        result.PlanCode.Should().Be("STARTER");
        result.BillingCycle.Should().Be("MONTHLY");
        result.MonthlyPrice.Should().Be(149m);
        result.IncludedCases.Should().Be(20);
        result.CurrentPeriodEnd.Should().BeAfter(result.CurrentPeriodStart);
    }

    [Fact]
    public async Task GetInvoice_ShouldReturnInvoiceSnapshot_WhenInvoiceBelongsToTenant()
    {
        var dbContext = CreateContext();
        var (tenantId, userId) = await SeedTenantAsync(dbContext, TenantStatus.Active, "STARTER", "MONTHLY");
        var invoiceId = await SeedBillingRecordAsync(dbContext, tenantId, "INV-00042", "STARTER", "MONTHLY");
        var service = CreateService(dbContext);

        var result = await service.GetInvoiceAsync(
            tenantId,
            userId,
            invoiceId,
            CancellationToken.None);

        result.TenantId.Should().Be(tenantId);
        result.InvoiceNumber.Should().Be("INV-00042");
        result.InvoiceSnapshot.Should().Contain("INV-00042");
    }

    [Fact]
    public async Task GetInvoice_ShouldThrowStateException_WhenInvoiceBelongsToDifferentTenant()
    {
        var dbContext = CreateContext();
        var (tenantId, userId) = await SeedTenantAsync(dbContext, TenantStatus.Active, "STARTER", "MONTHLY");
        var (otherTenantId, _) = await SeedTenantAsync(dbContext, TenantStatus.Active, "GROWTH", "ANNUAL");
        var otherInvoiceId = await SeedBillingRecordAsync(dbContext, otherTenantId, "INV-90001", "GROWTH", "ANNUAL");
        var service = CreateService(dbContext);

        var act = () => service.GetInvoiceAsync(
            tenantId,
            userId,
            otherInvoiceId,
            CancellationToken.None);

        await act.Should().ThrowAsync<TenantOnboardingStateException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task RegisterFailedPayment_ShouldMoveTenantToPaymentIssueAndScheduleRetry_WhenFirstFailure()
    {
        var dbContext = CreateContext();
        var (tenantId, userId) = await SeedTenantAsync(dbContext, TenantStatus.Active, "GROWTH", "MONTHLY");
        var service = CreateService(dbContext);

        var result = await service.RegisterFailedPaymentAsync(
            tenantId,
            userId,
            new RegisterFailedPaymentRequest
            {
                Reason = "Card processor declined charge for monthly invoice.",
                PaymentReference = "evt_failed_001"
            },
            CancellationToken.None);

        result.TenantStatus.Should().Be(TenantStatus.PaymentIssue);
        result.FailedPaymentAttempts.Should().Be(1);
        result.NextPaymentRetryAt.Should().NotBeNull();
        result.NextPaymentReminderAt.Should().NotBeNull();
        result.NextPaymentReminderAt!.Value.Should().BeBefore(result.NextPaymentRetryAt!.Value);
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "TenantPaymentFailed");
    }

    [Fact]
    public async Task ExecutePaymentRecovery_ShouldRestoreTenantToActive_WhenRetrySucceeds()
    {
        var dbContext = CreateContext();
        var (tenantId, userId) = await SeedTenantAsync(dbContext, TenantStatus.Active, "STARTER", "MONTHLY");
        var service = CreateService(dbContext);

        await service.RegisterFailedPaymentAsync(
            tenantId,
            userId,
            new RegisterFailedPaymentRequest
            {
                Reason = "SEPA debit failed because bank account had insufficient funds."
            },
            CancellationToken.None);

        var result = await service.ExecutePaymentRecoveryAsync(
            tenantId,
            userId,
            new ExecutePaymentRecoveryRequest
            {
                RetrySucceeded = true,
                ReminderSent = true
            },
            CancellationToken.None);

        result.TenantStatus.Should().Be(TenantStatus.Active);
        result.RecoveryComplete.Should().BeTrue();
        result.FailedPaymentAttempts.Should().Be(0);
        result.NextPaymentRetryAt.Should().BeNull();
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "TenantPaymentRecovered");
    }

    [Fact]
    public async Task ExecutePaymentRecovery_ShouldSuspendTenant_WhenRetryFailsThirdTime()
    {
        var dbContext = CreateContext();
        var (tenantId, userId) = await SeedTenantAsync(dbContext, TenantStatus.Active, "STARTER", "MONTHLY");
        var service = CreateService(dbContext);

        await service.RegisterFailedPaymentAsync(
            tenantId,
            userId,
            new RegisterFailedPaymentRequest
            {
                Reason = "Payment provider rejected charge due to card expiration."
            },
            CancellationToken.None);

        await service.ExecutePaymentRecoveryAsync(
            tenantId,
            userId,
            new ExecutePaymentRecoveryRequest
            {
                RetrySucceeded = false,
                ReminderSent = true,
                FailureReason = "First retry failed due to insufficient funds."
            },
            CancellationToken.None);

        var finalResult = await service.ExecutePaymentRecoveryAsync(
            tenantId,
            userId,
            new ExecutePaymentRecoveryRequest
            {
                RetrySucceeded = false,
                ReminderSent = true,
                FailureReason = "Second retry failed due to insufficient funds."
            },
            CancellationToken.None);

        finalResult.TenantStatus.Should().Be(TenantStatus.Suspended);
        finalResult.FailedPaymentAttempts.Should().Be(3);
        finalResult.NextPaymentRetryAt.Should().BeNull();
        finalResult.RecoveryComplete.Should().BeFalse();
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "TenantPaymentRetryFailed");
    }

    private static SanzuDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SanzuDbContext>()
            .UseInMemoryDatabase($"sanzu-billing-tests-{Guid.NewGuid()}")
            .Options;

        return new SanzuDbContext(options);
    }

    private static async Task<(Guid TenantId, Guid UserId)> SeedTenantAsync(
        SanzuDbContext dbContext,
        TenantStatus status,
        string plan,
        string billingCycle)
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var organization = new Organization
        {
            Id = tenantId,
            Name = $"Agency-{tenantId:N}",
            Location = "Lisbon",
            Status = status,
            SubscriptionPlan = plan,
            SubscriptionBillingCycle = billingCycle,
            SubscriptionActivatedAt = DateTime.UtcNow.AddDays(-20),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.Organizations.Add(organization);

        var user = new User
        {
            Id = userId,
            Email = $"admin-{userId:N}@agency.pt",
            FullName = "Agency Admin",
            OrgId = tenantId
        };
        dbContext.Users.Add(user);

        var role = new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleType = PlatformRole.AgencyAdmin,
            TenantId = tenantId,
            GrantedBy = userId
        };
        dbContext.UserRoles.Add(role);

        await dbContext.SaveChangesAsync();
        return (tenantId, userId);
    }

    private static async Task<Guid> SeedBillingRecordAsync(
        SanzuDbContext dbContext,
        Guid tenantId,
        string invoiceNumber,
        string plan,
        string billingCycle)
    {
        var nowUtc = DateTime.UtcNow;
        var billingRecord = new BillingRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceNumber = invoiceNumber,
            BillingCycleStart = nowUtc.AddMonths(-1),
            BillingCycleEnd = nowUtc,
            PlanCode = plan,
            BillingCycle = billingCycle,
            BaseAmount = 149m,
            OverageUnits = 0,
            OverageAmount = 0m,
            TaxRate = 0.23m,
            TaxAmount = 34.27m,
            TotalAmount = 183.27m,
            Currency = "EUR",
            Status = "FINALIZED",
            InvoiceSnapshot = $"{{\"invoiceNumber\":\"{invoiceNumber}\"}}",
            CreatedAt = nowUtc
        };

        dbContext.BillingRecords.Add(billingRecord);
        await dbContext.SaveChangesAsync();
        return billingRecord.Id;
    }

    private static TenantBillingService CreateService(SanzuDbContext dbContext)
    {
        return new TenantBillingService(
            new OrganizationRepository(dbContext),
            new UserRoleRepository(dbContext),
            new BillingRecordRepository(dbContext),
            new AuditRepository(dbContext),
            new EfUnitOfWork(dbContext),
            new RegisterFailedPaymentRequestValidator(),
            new ExecutePaymentRecoveryRequestValidator());
    }
}
