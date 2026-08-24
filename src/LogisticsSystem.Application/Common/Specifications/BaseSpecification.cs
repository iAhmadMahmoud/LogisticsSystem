using System.Linq.Expressions;

namespace LogisticsSystem.Application.Common.Specifications
{
    public abstract class BaseSpecification<TEntity> : ISpecification<TEntity>
    {
        protected BaseSpecification()
        {
        }

        protected BaseSpecification(Expression<Func<TEntity, bool>> criteria)
        {
            Criteria = criteria;
        }

        public Expression<Func<TEntity, bool>>? Criteria { get; private set; }
        public List<Expression<Func<TEntity, object>>> Includes { get; } = new();
        public Expression<Func<TEntity, object>>? OrderBy { get; private set; }
        public Expression<Func<TEntity, object>>? OrderByDescending { get; private set; }
        public int Take { get; private set; }
        public int Skip { get; private set; }
        public bool IsPagingEnabled { get; private set; }
        public bool IsNoTracking { get; private set; } = true;
        public bool IsSplitQuery { get; private set; } = false;

        protected void AsNoTracking(bool isNoTracking = true)
        {
            IsNoTracking = isNoTracking;
        }

        protected void AsSplitQuery(bool isSplitQuery = true)
        {
            IsSplitQuery = isSplitQuery;
        }

        protected void AddCriteria(Expression<Func<TEntity, bool>> criteria)
        {
            if (Criteria == null)
            {
                Criteria = criteria;
            }
            else
            {
                var param = Expression.Parameter(typeof(TEntity), "x");
                var body = Expression.AndAlso(
                    Expression.Invoke(Criteria, param),
                    Expression.Invoke(criteria, param)
                );
                Criteria = Expression.Lambda<Func<TEntity, bool>>(body, param);
            }
        }

        protected void AddInclude(Expression<Func<TEntity, object>> includeExpression)
        {
            Includes.Add(includeExpression);
        }

        protected void ApplyOrderBy(Expression<Func<TEntity, object>> orderByExpression)
        {
            OrderBy = orderByExpression;
            OrderByDescending = null;
        }

        protected void ApplyOrderByDescending(Expression<Func<TEntity, object>> orderByDescendingExpression)
        {
            OrderByDescending = orderByDescendingExpression;
            OrderBy = null;
        }

        protected void ApplyPaging(int skip, int take)
        {
            Skip = skip;
            Take = take;
            IsPagingEnabled = true;
        }
    }
}
