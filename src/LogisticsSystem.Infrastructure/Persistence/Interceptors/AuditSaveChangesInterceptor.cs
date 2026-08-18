using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LogisticsSystem.Infrastructure.Persistence.Interceptors
{
    public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentUserService _currentUserService;

        public AuditSaveChangesInterceptor(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            ApplyAuditInformation(eventData.Context);

            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            ApplyAuditInformation(eventData.Context);

            return base.SavingChangesAsync(
                eventData,
                result,
                cancellationToken);
        }

        private void ApplyAuditInformation(DbContext? context)
        {
            if (context is null)
                return;

            context.ChangeTracker.DetectChanges();

            // During unauthenticated operations (e.g. login, register) there is no
            // current user yet. Safely attempt to read the ID and skip audit fields
            // when none is available instead of throwing.
            Guid? currentUserId = null;
            try
            {
                var id = _currentUserService.UserId;
                if (id != Guid.Empty)
                    currentUserId = id;
            }
            catch (UnauthorizedAccessException)
            {
                // No authenticated user — audit fields will be left unset.
            }

            foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;

                    if (currentUserId.HasValue)
                        entry.Entity.CreatedBy = currentUserId.Value.ToString();
                }

                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;

                    if (currentUserId.HasValue)
                        entry.Entity.UpdatedBy = currentUserId.Value.ToString();
                }
            }
        }
    }
}
