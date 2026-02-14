using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface ITenantBillingService
{
    Task<BillingHistoryResponse> GetBillingHistoryAsync(
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<BillingUsageSummaryResponse> GetUsageSummaryAsync(
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<InvoiceDownloadResponse> GetInvoiceAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid invoiceId,
        CancellationToken cancellationToken);

    Task<BillingRecordResponse> CreateBillingRecordAsync(
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<PaymentRecoveryStatusResponse> RegisterFailedPaymentAsync(
        Guid tenantId,
        Guid actorUserId,
        RegisterFailedPaymentRequest request,
        CancellationToken cancellationToken);

    Task<PaymentRecoveryStatusResponse> ExecutePaymentRecoveryAsync(
        Guid tenantId,
        Guid actorUserId,
        ExecutePaymentRecoveryRequest request,
        CancellationToken cancellationToken);
}
