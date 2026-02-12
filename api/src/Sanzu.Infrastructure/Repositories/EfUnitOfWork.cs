using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly SanzuDbContext _dbContext;

    public EfUnitOfWork(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        if (_dbContext.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true)
        {
            await action(cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await action(cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            if (IsUniqueEmailConstraintViolation(exception))
            {
                throw new DuplicateEmailException();
            }

            throw;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static bool IsUniqueEmailConstraintViolation(DbUpdateException exception)
    {
        var sqlException = FindSqlException(exception);
        if (sqlException is not null)
        {
            if (sqlException.Number is not (2601 or 2627))
            {
                return false;
            }

            return ContainsUserEmailHint(sqlException.Message);
        }

        var fullText = exception.ToString();
        return ContainsUserEmailHint(fullText)
            && (fullText.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
                || fullText.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                || fullText.Contains("2601", StringComparison.OrdinalIgnoreCase)
                || fullText.Contains("2627", StringComparison.OrdinalIgnoreCase));
    }

    private static SqlException? FindSqlException(Exception exception)
    {
        Exception? current = exception;
        while (current is not null)
        {
            if (current is SqlException sqlException)
            {
                return sqlException;
            }

            current = current.InnerException;
        }

        return null;
    }

    private static bool ContainsUserEmailHint(string text)
    {
        return text.Contains("IX_Users_Email", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Users.Email", StringComparison.OrdinalIgnoreCase)
            || (text.Contains("Users", StringComparison.OrdinalIgnoreCase)
                && text.Contains("Email", StringComparison.OrdinalIgnoreCase));
    }
}
