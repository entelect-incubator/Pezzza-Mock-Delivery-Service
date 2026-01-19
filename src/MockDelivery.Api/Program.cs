namespace MockDelivery.Api;

using MockDelivery.Api.Endpoints;
using MockDelivery.Api.Services;
using MockDelivery.Api.Workers;
using Serilog;

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            Log.Information("Starting Pezzza Mock Delivery Service");

            var builder = WebApplication.CreateBuilder(args);

            // Add Serilog
            builder.Host.UseSerilog((context, configuration) =>
                configuration.ReadFrom.Configuration(context.Configuration));

            // Load settings
            var settings = new MockDeliverySettings();
            builder.Configuration.GetSection("MockDelivery").Bind(settings);
            builder.Services.AddSingleton(settings);

            // Add services
            builder.Services.AddSingleton<IDeliveryStore, DeliveryStore>();
            builder.Services.AddScoped<IWebhookService, WebhookService>();
            builder.Services.AddHttpClient();

            // Add background worker
            builder.Services.AddHostedService<DeliverySimulationWorker>();

            // Add API versioning
            builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            });

            // Add OpenAPI
            if (settings.OpenApi.Enable)
            {
                builder.Services.AddOpenApi();
            }

            // Add health checks
            builder.Services.AddHealthChecks();

            // Add CORS
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            // Configure middleware
            if (app.Environment.IsDevelopment())
            {
                if (settings.OpenApi.Enable)
                {
                    app.MapOpenApi();
                    app.MapScalarApiReference(options =>
                    {
                        options.WithTitle(settings.OpenApi.Title)
                               .WithTheme(ScalarTheme.Mars);
                    });
                }
            }

            app.UseSerilogRequestLogging();
            app.UseCors();

            // Map endpoints
            app.MapDeliveryEndpoints();
            app.MapHealthChecks("/health");

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
