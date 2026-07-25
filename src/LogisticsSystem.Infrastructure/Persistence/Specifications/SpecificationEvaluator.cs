using LogisticsSystem.Application.Common.Specifications;
using Microsoft.EntityFrameworkCore;

namespace LogisticsSystem.Infrastructure.Persistence.Specifications
{
    public static class SpecificationEvaluator<TEntity> where TEntity : class
    {
        public static IQueryable<TEntity> GetQuery(
            IQueryable<TEntity> inputQuery,
            ISpecification<TEntity> specification,
            bool evaluatePaging = true)
        {
            var query = inputQuery;

            if (specification.Criteria != null)
            {
                query = query.Where(specification.Criteria);
            }

            query = specification.Includes.Aggregate(
                query,
                (current, include) => current.Include(include));

            if (specification.OrderBy != null)
            {
                query = query.OrderBy(specification.OrderBy);
            }
            else if (specification.OrderByDescending != null)
            {
                query = query.OrderByDescending(specification.OrderByDescending);
            }

            if (evaluatePaging && specification.IsPagingEnabled)
            {
                query = query.Skip(specification.Skip).Take(specification.Take);
            }

            return query;
        }
    }
}
