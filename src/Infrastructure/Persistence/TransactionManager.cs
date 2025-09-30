using Microsoft.EntityFrameworkCore;
using Sigma.Domain.Contracts;

namespace Sigma.Infrastructure.Persistence;

public class TransactionManager : ITransactionManager
{
    private readonly SigmaDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;

    public TransactionManager(SigmaDbContext dbContext, IUnitOfWork unitOfWork)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public bool HasActiveTransaction => _dbContext.Database.CurrentTransaction != null;

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        if (HasActiveTransaction)
        {
            return await operation(cancellationToken);
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var result = await operation(cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}