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
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}");
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

        if (app.Environment.IsDevelopment())
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

        app.MapHub<NotificationHub>("/hubs/notifications");
        app.MapHub<TrackingHub>("/hubs/tracking");

        app.Run();
    }
}

