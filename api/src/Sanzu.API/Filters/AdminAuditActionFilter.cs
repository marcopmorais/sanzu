using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Sanzu.Core.Entities;
using Sanzu.Core.Interfaces;

namespace Sanzu.API.Filters;

public sealed class AdminAuditActionFilter : IAsyncActionFilter
{
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdminAuditActionFilter(IAuditRepository auditRepository, IUnitOfWork unitOfWork)
    {
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var result = await next();

        if (!IsAdminRoute(context))
            return;

        if (HasSkipAuditAttribute(context))
            return;

        if (result.Exception != null && !result.ExceptionHandled)
            return;

        var actorUserId = GetActorUserId(context.HttpContext.User);
        var tenantId = GetTenantIdFromRoute(context);
        var eventType = DeriveEventType(context);
        var metadata = BuildMetadata(context, tenantId);

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                await _auditRepository.CreateAsync(
                    new AuditEvent
                    {
                        Id = Guid.NewGuid(),
                        ActorUserId = actorUserId,
                        EventType = eventType,
                        Metadata = metadata,
                        CreatedAt = DateTime.UtcNow
                    },
                    ct);
            },
            context.HttpContext.RequestAborted);
    }

    private static bool IsAdminRoute(ActionExecutingContext context)
    {
        var path = context.HttpContext.Request.Path.Value ?? string.Empty;
        return path.StartsWith("/api/v1/admin/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasSkipAuditAttribute(ActionExecutingContext context)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor descriptor)
            return false;

        return descriptor.MethodInfo.GetCustomAttributes(typeof(SkipAdminAuditAttribute), false).Length > 0;
    }

    private static Guid GetActorUserId(ClaimsPrincipal user)
    {
        var userIdValue =
            user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? user.FindFirstValue("user_id");

        return Guid.TryParse(userIdValue, out var userId) ? userId : Guid.Empty;
    }

    private static Guid? GetTenantIdFromRoute(ActionExecutingContext context)
    {
        if (context.RouteData.Values.TryGetValue("tenantId", out var tenantIdValue)
            && Guid.TryParse(tenantIdValue?.ToString(), out var tenantId))
        {
            return tenantId;
        }

        return null;
    }

    private static string DeriveEventType(ActionExecutingContext context)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor descriptor)
            return "Admin.Unknown.Action";

        var controllerName = descriptor.ControllerName;
        var actionName = descriptor.ActionName;

        // Strip "Admin" prefix if present
        var entity = controllerName;
        if (entity.StartsWith("Admin", StringComparison.Ordinal) && entity.Length > 5)
            entity = entity[5..];

        return $"Admin.{entity}.{actionName}";
    }

    private static string BuildMetadata(ActionExecutingContext context, Guid? tenantId)
    {
        var meta = new Dictionary<string, object?>
        {
            ["path"] = context.HttpContext.Request.Path.Value,
            ["method"] = context.HttpContext.Request.Method
        };

        if (tenantId.HasValue)
            meta["tenantId"] = tenantId.Value;

        return JsonSerializer.Serialize(meta);
    }
}
