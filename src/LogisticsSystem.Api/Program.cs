using Hangfire;
using LogisticsSystem.Api.Common.Extensions;
using LogisticsSystem.Application;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Infrastructure;
using Microsoft.OpenApi;

namespace LogisticsSystem.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Logistics System API",
                Version = "v1"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
            });
        });

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        builder.Services.AddProblemDetails();

        var app = builder.Build();

        app.UseHangfireDashboard("/hangfire");
        RecurringJob.AddOrUpdate<IAssignmentExpirationService>("expire-dispatch-assignments", service => service.ExpireAssignmentsAsync(CancellationToken.None), Cron.Minutely);

        using (var scope = app.Services.CreateScope())
        {
            var dbInitializer = scope.ServiceProvider.GetRequiredService<LogisticsSystem.Infrastructure.Persistence.Seed.DbInitializer>();
            await dbInitializer.InitializeAsync();
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseExceptionHandler();

        app.UseAuthentication();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
