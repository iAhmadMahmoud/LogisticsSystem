using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Domain.Common;

namespace LogisticsSystem.Application.Common.Interfaces.Persistence
{
    public interface IGenericRepository<TEntity> where TEntity : BaseEntity
    {
        Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        void Update(TEntity entity);
        void Delete(TEntity entity);
        IQueryable<TEntity> AsQueryable();

        Task<IReadOnlyList<TEntity>> ListAsync( ISpecification<TEntity> specification, CancellationToken cancellationToken = default);

        Task<int> CountAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);

        Task<TEntity?> FirstOrDefaultAsync( ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
    }
}
