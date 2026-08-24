using Hangfire;
using LogisticsSystem.Api.Common.Extensions;
using LogisticsSystem.Application;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Infrastructure;
using LogisticsSystem.Infrastructure.BackgroundJobs;
using LogisticsSystem.Infrastructure.SignalR;
using Microsoft.OpenApi;
using Serilog;

namespace LogisticsSystem.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "LogisticsSystem.Api")
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] ({CorrelationId}) {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: 10 * 1024 * 1024,
                    retainedFileCountLimit: 14,
                    rollOnFileSizeLimit: true,
                    shared: true,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({SourceContext}) [{CorrelationId}] {Message:lj}{NewLine}{Exception}");
        });

        builder.Services.AddControllers()
        .AddJsonOptions(options =>
         {
             options.JsonSerializerOptions.Converters.Add(
                 new System.Text.Json.Serialization.JsonStringEnumConverter());
         });

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
        builder.Services.AddCustomCors(builder.Configuration, builder.Environment);
        builder.Services.AddCustomRateLimiting(builder.Configuration);

        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        builder.Services.AddProblemDetails();

        var app = builder.Build();

        app.UseMiddleware<LogisticsSystem.Api.Common.Middleware.CorrelationIdMiddleware>();

        app.UseSerilogRequestLogging();

        if (!app.Environment.IsEnvironment("Testing"))
        {
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { new HangfireAuthorizationFilter() },
                DashboardTitle = "Logistics System Background Jobs",
                AppPath = "/swagger"
            });
            RecurringJob.AddOrUpdate<IAssignmentExpirationService>("expire-dispatch-assignments", service => service.ExpireAssignmentsAsync(CancellationToken.None), Cron.Minutely);

            using (var scope = app.Services.CreateScope())
            {
                var dbInitializer = scope.ServiceProvider.GetRequiredService<LogisticsSystem.Infrastructure.Persistence.Seed.DbInitializer>();
                await dbInitializer.InitializeAsync();
            }
        }

        if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:EnabledInProduction", false))
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseExceptionHandler();

        app.UseCors(CorsPolicies.Default);

        app.UseRateLimiter();

        app.UseAuthentication();

        app.UseAuthorization();


        app.MapControllers();

        app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = LogisticsSystem.Api.Common.Health.HealthCheckResponseWriter.WriteDetailedResponse
        });

        app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = LogisticsSystem.Api.Common.Health.HealthCheckResponseWriter.WriteMinimalResponse
        });

        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = LogisticsSystem.Api.Common.Health.HealthCheckResponseWriter.WriteDetailedResponse
        });

        app.MapHub<NotificationHub>("/hubs/notifications");
        app.MapHub<TrackingHub>("/hubs/tracking");

        app.Run();
    }
}

