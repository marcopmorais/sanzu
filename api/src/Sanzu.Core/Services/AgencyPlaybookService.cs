using System.Text.Json;
using FluentValidation;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Services;

public sealed class AgencyPlaybookService : IAgencyPlaybookService
{
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IAgencyPlaybookRepository _playbookRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreatePlaybookRequest> _createValidator;
    private readonly IValidator<UpdatePlaybookRequest> _updateValidator;

    public AgencyPlaybookService(
        IUserRoleRepository userRoleRepository,
        IAgencyPlaybookRepository playbookRepository,
        IAuditRepository auditRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreatePlaybookRequest> createValidator,
        IValidator<UpdatePlaybookRequest> updateValidator)
    {
        _userRoleRepository = userRoleRepository;
        _playbookRepository = playbookRepository;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IReadOnlyList<PlaybookResponse>> ListAsync(
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var playbooks = await _playbookRepository.ListAsync(tenantId, cancellationToken);
        return playbooks.Select(Map).ToList();
    }

    public async Task<PlaybookResponse> GetByIdAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid playbookId,
        CancellationToken cancellationToken)
    {
        var playbook = await _playbookRepository.GetByIdAsync(tenantId, playbookId, cancellationToken);
        if (playbook is null)
        {
            throw new CaseStateException("Playbook not found.");
        }

        return Map(playbook);
    }

    public async Task<PlaybookResponse> CreateAsync(
        Guid tenantId,
        Guid actorUserId,
        CreatePlaybookRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        await EnsureAgencyAdminAsync(actorUserId, tenantId, cancellationToken);

        PlaybookResponse? response = null;
        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var existing = await _playbookRepository.ListAsync(tenantId, token);
                var maxVersion = existing.Count > 0 ? existing.Max(p => p.Version) : 0;

                var playbook = new AgencyPlaybook
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim(),
                    Version = maxVersion + 1,
                    Status = PlaybookStatus.Draft,
                    ChangeNotes = request.ChangeNotes?.Trim(),
                    CreatedByUserId = actorUserId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _playbookRepository.CreateAsync(playbook, token);
                await WriteAuditAsync(actorUserId, "PlaybookCreated", new
                {
                    TenantId = tenantId,
                    PlaybookId = playbook.Id,
                    playbook.Name,
                    playbook.Version
                }, token);

                response = Map(playbook);
            },
            cancellationToken);

        return response!;
    }

    public async Task<PlaybookResponse> UpdateAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid playbookId,
        UpdatePlaybookRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        await EnsureAgencyAdminAsync(actorUserId, tenantId, cancellationToken);

        PlaybookResponse? response = null;
        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var playbook = await _playbookRepository.GetByIdAsync(tenantId, playbookId, token);
                if (playbook is null)
                {
                    throw new CaseStateException("Playbook not found.");
                }

                if (playbook.Status is not (PlaybookStatus.Draft or PlaybookStatus.InReview))
                {
                    throw new CaseStateException("Only Draft or InReview playbooks can be edited.");
                }

                if (request.Name is not null) playbook.Name = request.Name.Trim();
                if (request.Description is not null) playbook.Description = request.Description.Trim();
                if (request.ChangeNotes is not null) playbook.ChangeNotes = request.ChangeNotes.Trim();
                if (request.Status.HasValue) playbook.Status = request.Status.Value;
                playbook.UpdatedAt = DateTime.UtcNow;

                await _playbookRepository.UpdateAsync(playbook, token);
                await WriteAuditAsync(actorUserId, "PlaybookUpdated", new
                {
                    TenantId = tenantId,
                    PlaybookId = playbook.Id,
                    playbook.Name,
                    playbook.Version,
                    Status = playbook.Status.ToString()
                }, token);

                response = Map(playbook);
            },
            cancellationToken);

        return response!;
    }

    public async Task<PlaybookResponse> ActivateAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid playbookId,
        CancellationToken cancellationToken)
    {
        await EnsureAgencyAdminAsync(actorUserId, tenantId, cancellationToken);

        PlaybookResponse? response = null;
        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var playbook = await _playbookRepository.GetByIdAsync(tenantId, playbookId, token);
                if (playbook is null)
                {
                    throw new CaseStateException("Playbook not found.");
                }

                // Archive the currently active version
                var currentActive = await _playbookRepository.GetActiveAsync(tenantId, token);
                if (currentActive is not null && currentActive.Id != playbookId)
                {
                    currentActive.Status = PlaybookStatus.Archived;
                    currentActive.UpdatedAt = DateTime.UtcNow;
                    await _playbookRepository.UpdateAsync(currentActive, token);

                    await WriteAuditAsync(actorUserId, "PlaybookArchived", new
                    {
                        TenantId = tenantId,
                        PlaybookId = currentActive.Id,
                        currentActive.Name,
                        currentActive.Version
                    }, token);
                }

                playbook.Status = PlaybookStatus.Active;
                playbook.ActivatedByUserId = actorUserId;
                playbook.ActivatedAt = DateTime.UtcNow;
                playbook.UpdatedAt = DateTime.UtcNow;

                await _playbookRepository.UpdateAsync(playbook, token);
                await WriteAuditAsync(actorUserId, "PlaybookActivated", new
                {
                    TenantId = tenantId,
                    PlaybookId = playbook.Id,
                    playbook.Name,
                    playbook.Version
                }, token);

                response = Map(playbook);
            },
            cancellationToken);

        return response!;
    }

    private async Task EnsureAgencyAdminAsync(Guid actorUserId, Guid tenantId, CancellationToken cancellationToken)
    {
        var isAdmin = await _userRoleRepository.HasRoleAsync(
            actorUserId,
            tenantId,
            PlatformRole.AgencyAdmin,
            cancellationToken);

        if (!isAdmin)
        {
            throw new TenantAccessDeniedException();
        }
    }

    private static PlaybookResponse Map(AgencyPlaybook playbook)
    {
        return new PlaybookResponse
        {
            Id = playbook.Id,
            TenantId = playbook.TenantId,
            Name = playbook.Name,
            Description = playbook.Description,
            Version = playbook.Version,
            Status = playbook.Status,
            ChangeNotes = playbook.ChangeNotes,
            CreatedByUserId = playbook.CreatedByUserId,
            ActivatedByUserId = playbook.ActivatedByUserId,
            CreatedAt = playbook.CreatedAt,
            UpdatedAt = playbook.UpdatedAt,
            ActivatedAt = playbook.ActivatedAt
        };
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
