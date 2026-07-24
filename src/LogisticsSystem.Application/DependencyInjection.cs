using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace LogisticsSystem.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {

            var assembly =  Assembly.GetExecutingAssembly();

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(assembly);
            });

            services.AddValidatorsFromAssembly(assembly);

            return services;
        }
    }
}
