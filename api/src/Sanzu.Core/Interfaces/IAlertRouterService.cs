namespace Sanzu.Core.Interfaces;

public interface IAlertRouterService
{
    Task<IReadOnlyList<Guid>> ResolveRecipientsAsync(string targetRole, bool includeSanzuAdmin, CancellationToken cancellationToken);
}
