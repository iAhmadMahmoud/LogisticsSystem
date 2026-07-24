using FluentValidation;
using LogisticsSystem.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace LogisticsSystem.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {


            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            });

            services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

            services.AddTransient(
                typeof(IPipelineBehavior<,>),
                typeof(ValidationBehavior<,>));

            services.AddAutoMapper(typeof(DependencyInjection).Assembly);

            return services;
        }
    }
}
