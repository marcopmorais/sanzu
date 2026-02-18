using System.Security.Cryptography;
using System.Text;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;
using System.Text.RegularExpressions;

namespace Sanzu.Core.Services;

public sealed class CaseAuditExportService : ICaseAuditExportService
{
    private readonly ICaseRepository _caseRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly ICaseDocumentRepository _caseDocumentRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IUnitOfWork _unitOfWork;

    private const string RedactedPlaceholder = "[REDACTED]";

    public CaseAuditExportService(
        ICaseRepository caseRepository,
        IAuditRepository auditRepository,
        ICaseDocumentRepository caseDocumentRepository,
        IUserRoleRepository userRoleRepository,
        IUnitOfWork unitOfWork)
    {
        _caseRepository = caseRepository;
        _auditRepository = auditRepository;
        _caseDocumentRepository = caseDocumentRepository;
        _userRoleRepository = userRoleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CaseAuditExportResponse> ExportAsync(
        Guid tenantId,
        Guid caseId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var hasAdminRole = await _userRoleRepository.HasRoleAsync(
            actorUserId, tenantId, PlatformRole.AgencyAdmin, cancellationToken);

        if (!hasAdminRole)
        {
            throw new TenantAccessDeniedException();
        }

        var caseEntity = await _caseRepository.GetByIdAsync(caseId, cancellationToken);

        if (caseEntity == null || caseEntity.TenantId != tenantId)
        {
            throw new CaseStateException("Case not found.");
        }

        var auditEvents = await _auditRepository.GetByCaseIdAsync(caseId, cancellationToken);
        var documents = await _caseDocumentRepository.GetByCaseIdAsync(caseId, cancellationToken);

        var generatedAt = DateTime.UtcNow;
        var exportId = GenerateDeterministicExportId(tenantId, caseId, generatedAt);

        var caseSummary = BuildCaseSummary(caseEntity);
        var exportEvents = BuildAuditEvents(auditEvents);
        var evidenceReferences = BuildEvidenceReferences(documents);

        // Record the export as an audit event
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _auditRepository.CreateAsync(new AuditEvent
            {
                Id = Guid.NewGuid(),
                CaseId = caseId,
                ActorUserId = actorUserId,
                EventType = "ExportGenerated",
                Metadata = $"{{\"exportId\":\"{exportId}\"}}",
                CreatedAt = generatedAt
            }, ct);
        }, cancellationToken);

        return new CaseAuditExportResponse
        {
            ExportId = exportId,
            CaseId = caseId,
            TenantId = tenantId,
            GeneratedAt = generatedAt,
            GeneratedByUserId = actorUserId.ToString(),
            CaseSummary = caseSummary,
            AuditEvents = exportEvents,
            EvidenceReferences = evidenceReferences
        };
    }

    private static string GenerateDeterministicExportId(Guid tenantId, Guid caseId, DateTime generatedAt)
    {
        var input = $"{tenantId}:{caseId}:{generatedAt:O}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }

    private static CaseAuditExportSummary BuildCaseSummary(Case caseEntity)
    {
        return new CaseAuditExportSummary
        {
            CaseNumber = caseEntity.CaseNumber,
            DeceasedFullName = caseEntity.DeceasedFullName,
            DateOfDeath = caseEntity.DateOfDeath,
            CaseType = caseEntity.CaseType,
            Urgency = caseEntity.Urgency,
            Status = caseEntity.Status.ToString(),
            PlaybookId = caseEntity.PlaybookId,
            PlaybookVersion = caseEntity.PlaybookVersion,
            CreatedAt = caseEntity.CreatedAt,
            ClosedAt = caseEntity.ClosedAt
        };
    }

    private static IReadOnlyList<CaseAuditExportEvent> BuildAuditEvents(IReadOnlyList<AuditEvent> auditEvents)
    {
        return auditEvents
            .OrderBy(e => e.CreatedAt)
            .Select(e => new CaseAuditExportEvent
            {
                EventId = e.Id,
                EventType = e.EventType,
                ActorUserId = e.ActorUserId.ToString(),
                Metadata = RedactSensitiveMetadata(e.Metadata),
                CreatedAt = e.CreatedAt
            })
            .ToList();
    }

    private static IReadOnlyList<CaseAuditExportEvidenceReference> BuildEvidenceReferences(
        IReadOnlyList<CaseDocument> documents)
    {
        return documents
            .OrderBy(d => d.CreatedAt)
            .Select(d => new CaseAuditExportEvidenceReference
            {
                DocumentId = d.Id,
                FileName = d.FileName,
                ContentType = d.ContentType,
                SizeBytes = d.SizeBytes,
                Classification = d.Classification.ToString(),
                CurrentVersionNumber = d.CurrentVersionNumber,
                UploadedAt = d.CreatedAt
            })
            .ToList();
    }

    private static string RedactSensitiveMetadata(string metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return metadata;
        }

        // Redact known sensitive patterns (email, phone) in metadata JSON
        var result = Regex.Replace(
            metadata,
            @"""(email|phone|ssn|nif|taxId)"":\s*""[^""]*""",
            match =>
            {
                var key = match.Value.Split(':')[0];
                return $"{key}: \"{RedactedPlaceholder}\"";
            },
            RegexOptions.IgnoreCase);

        return result;
    }
}
