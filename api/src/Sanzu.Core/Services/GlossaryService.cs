using System.Text.Json;
using FluentValidation;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Services;

public sealed class GlossaryService : IGlossaryService
{
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IGlossaryTermRepository _glossaryTermRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpsertGlossaryTermRequest> _upsertValidator;

    public GlossaryService(
        IUserRoleRepository userRoleRepository,
        IGlossaryTermRepository glossaryTermRepository,
        IAuditRepository auditRepository,
        IUnitOfWork unitOfWork,
        IValidator<UpsertGlossaryTermRequest> upsertValidator)
    {
        _userRoleRepository = userRoleRepository;
        _glossaryTermRepository = glossaryTermRepository;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
        _upsertValidator = upsertValidator;
    }

    public async Task<GlossaryLookupResponse> SearchAsync(
        Guid tenantId,
        Guid actorUserId,
        string? query,
        string? locale,
        CancellationToken cancellationToken)
    {
        var normalizedLocale = NormalizeLocale(locale);
        var visibility = await ResolveVisibilityAsync(tenantId, actorUserId, cancellationToken);

        if (string.IsNullOrWhiteSpace(query))
        {
            return new GlossaryLookupResponse();
        }

        var terms = await _glossaryTermRepository.SearchAsync(tenantId, query, normalizedLocale, cancellationToken);
        var filtered = terms.Where(x => IsVisibleTo(visibility, x.Visibility)).ToList();
        return new GlossaryLookupResponse { Terms = filtered.Select(Map).ToList() };
    }

    public async Task<GlossaryTermResponse> GetTermAsync(
        Guid tenantId,
        Guid actorUserId,
        string key,
        string? locale,
        CancellationToken cancellationToken)
    {
        var normalizedLocale = NormalizeLocale(locale);
        var visibility = await ResolveVisibilityAsync(tenantId, actorUserId, cancellationToken);

        var term = await _glossaryTermRepository.GetByKeyAsync(tenantId, key.Trim(), normalizedLocale, cancellationToken);
        if (term is null || !IsVisibleTo(visibility, term.Visibility))
        {
            // Return not-found for non-visible terms to avoid leaking existence.
            throw new CaseStateException("Glossary term not found.");
        }

        return Map(term);
    }

    public async Task<GlossaryTermResponse> UpsertAsync(
        Guid tenantId,
        Guid actorUserId,
        string key,
        UpsertGlossaryTermRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _upsertValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var hasTenantAdminRole = await _userRoleRepository.HasRoleAsync(
            actorUserId,
            tenantId,
            PlatformRole.AgencyAdmin,
            cancellationToken);

        if (!hasTenantAdminRole)
        {
            throw new TenantAccessDeniedException();
        }

        GlossaryTermResponse? response = null;
        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var normalizedLocale = NormalizeLocale(request.Locale);
                var normalizedKey = key.Trim();
                var existing = await _glossaryTermRepository.GetByKeyAsync(tenantId, normalizedKey, normalizedLocale, token);

                if (existing is null)
                {
                    var created = new GlossaryTerm
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        Key = normalizedKey,
                        Term = request.Term.Trim(),
                        Definition = request.Definition.Trim(),
                        WhyThisMatters = request.WhyThisMatters?.Trim(),
                        Locale = normalizedLocale,
                        Visibility = request.Visibility,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _glossaryTermRepository.CreateAsync(created, token);
                    await WriteAuditAsync(
                        actorUserId,
                        "GlossaryTermCreated",
                        new
                        {
                            TenantId = tenantId,
                            Key = created.Key,
                            Locale = created.Locale,
                            Visibility = created.Visibility.ToString()
                        },
                        token);

                    response = Map(created);
                    return;
                }

                existing.Term = request.Term.Trim();
                existing.Definition = request.Definition.Trim();
                existing.WhyThisMatters = request.WhyThisMatters?.Trim();
                existing.Visibility = request.Visibility;
                existing.UpdatedAt = DateTime.UtcNow;

                await _glossaryTermRepository.UpdateAsync(existing, token);
                await WriteAuditAsync(
                    actorUserId,
                    "GlossaryTermUpdated",
                    new
                    {
                        TenantId = tenantId,
                        Key = existing.Key,
                        Locale = existing.Locale,
                        Visibility = existing.Visibility.ToString()
                    },
                    token);

                response = Map(existing);
            },
            cancellationToken);

        return response!;
    }

    private async Task<GlossaryVisibility> ResolveVisibilityAsync(
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (await _userRoleRepository.HasRoleAsync(actorUserId, tenantId, PlatformRole.SanzuAdmin, cancellationToken))
        {
            return GlossaryVisibility.AdminOnly;
        }

        if (await _userRoleRepository.HasRoleAsync(actorUserId, tenantId, PlatformRole.AgencyAdmin, cancellationToken))
        {
            return GlossaryVisibility.AgencyOnly;
        }

        return GlossaryVisibility.Public;
    }

    private static bool IsVisibleTo(GlossaryVisibility viewer, GlossaryVisibility termVisibility)
    {
        // Viewer is the maximum visibility they can access.
        // Public sees only Public.
        // AgencyOnly sees Public + AgencyOnly.
        // AdminOnly sees everything.
        return viewer switch
        {
            GlossaryVisibility.AdminOnly => true,
            GlossaryVisibility.AgencyOnly => termVisibility is GlossaryVisibility.Public or GlossaryVisibility.AgencyOnly,
            _ => termVisibility is GlossaryVisibility.Public
        };
    }

    private static GlossaryTermResponse Map(GlossaryTerm term)
    {
        return new GlossaryTermResponse
        {
            Key = term.Key,
            Term = term.Term,
            Definition = term.Definition,
            WhyThisMatters = term.WhyThisMatters,
            Locale = term.Locale,
            Visibility = term.Visibility,
            UpdatedAt = term.UpdatedAt
        };
    }

    private static string NormalizeLocale(string? locale)
    {
        return string.IsNullOrWhiteSpace(locale) ? "pt-PT" : locale.Trim();
    }

    private Task WriteAuditAsync(
        Guid actorUserId,
        string eventType,
        object metadata,
        CancellationToken cancellationToken)
    {
        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            EventType = eventType,
            Metadata = JsonSerializer.Serialize(metadata)
        };

        return _auditRepository.CreateAsync(auditEvent, cancellationToken);
    }
}

