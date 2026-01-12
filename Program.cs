using System.Diagnostics;
using Microsoft.OpenApi.Models;
using Nest;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;

namespace Logging_ELK
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            Log.Logger = new LoggerConfiguration()
                // C?U HÌNH M?C ?? LOG
                .MinimumLevel.Debug()  
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) 
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning) 

                // ENRICHMENT - THÊM THÔNG TIN B? SUNG VÀO LOG
                .Enrich.FromLogContext() 
                .Enrich.WithProperty("Application", "Logging-ELK-API")
                .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
             

                // WRITE TO OPENTELEMETRY - GHI LOG ??N OTEL COLLECTOR
                .WriteTo.OpenTelemetry(options =>
                {
                    // Endpoint c?a OpenTelemetry Collector
                    options.Endpoint = builder.Configuration["Serilog:OpenTelemetry:Endpoint"]
                        ?? "http://localhost:4318/v1/logs"; // M?c ??nh localhost cho development

                    // Protocol: có th? dùng gRPC ho?c HTTP Protobuf
                    options.Protocol = builder.Configuration["Serilog:OpenTelemetry:Protocol"] == "grpc"
                        ? OtlpProtocol.Grpc
                        : OtlpProtocol.HttpProtobuf;

                    // ATTRIBUTES - Thông tin resource s? ???c g?i kèm logs
                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = builder.Configuration["OpenTelemetry:ServiceName"] ?? "logging-elk-api",
                        ["service.version"] = builder.Configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0",
                        ["service.instance.id"] = Environment.MachineName,
                        ["deployment.environment"] = builder.Environment.EnvironmentName,
                        ["telemetry.sdk.name"] = "serilog",
                        ["telemetry.sdk.language"] = "dotnet",
                        ["telemetry.sdk.version"] = typeof(LoggerConfiguration).Assembly.GetName().Version?.ToString() ?? "unknown"
                    };

                    // HEADERS - N?u c?n authentication
                    var headers = new Dictionary<string, string>();
                    var authToken = builder.Configuration["Serilog:OpenTelemetry:AuthorizationToken"];
                    if (!string.IsNullOrEmpty(authToken))
                    {
                        headers["Authorization"] = $"Bearer {authToken}";
                    }
                    options.Headers = headers;

                    // BATCH CONFIGURATION - C?u hình batch ?? t?i ?u hi?u su?t
                    options.BatchingOptions.BatchSizeLimit = 100; // S? log t?i ?a trong 1 batch
                    //options. = TimeSpan.FromSeconds(2); // G?i batch m?i 2 giây
                    options.BatchingOptions.QueueLimit = 10000; // S? log t?i ?a trong queue

                    // INCLUDED DATA - D? li?u nào s? ???c g?i kèm
                    options.IncludedData = IncludedData.MessageTemplateTextAttribute |
                                           IncludedData.TraceIdField |
                                           IncludedData.SpanIdField ;

                  
                })

                //// WRITE TO FILE - GHI LOG VÀO FILE (backup/fallback)
                //.WriteTo.File(
                //    path: "logs/log-.txt", // File log v?i tên có date
                //    rollingInterval: RollingInterval.Day, // T?o file m?i m?i ngày
                //    retainedFileCountLimit: 7, // Gi? 7 file log c?
                //    shared: true, // Cho phép chia s? file gi?a các process
                //    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                //    restrictedToMinimumLevel: LogEventLevel.Warning) // Ch? ghi Warning tr? lên vào file

                .CreateLogger();

            try
            {

                // S? d?ng Serilog cho logging
                builder.Host.UseSerilog();

                builder.Services.AddControllers();




                // Swagger/OpenAPI
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Title = "Logging ELK API",
                        Version = "v1",
                        Description = "API v?i logging tích h?p Serilog + OpenTelemetry + ELK Stack"
                    });
                });

                // HTTP Client
                builder.Services.AddHttpClient();
                var app = builder.Build();
                // Development tools
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI(c =>
                    {
                        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Logging ELK API v1");
                        c.RoutePrefix = "swagger";
                    });
                }

                // HTTPS redirection
                app.UseHttpsRedirection();
                // Authorization
                app.UseAuthorization();

                // Map controllers
                app.MapControllers();

                // Health check endpoint
                app.MapGet("/health", () =>
                {
                    Log.Information("Health check called");
                    return Results.Ok(new
                    {
                        status = "healthy",
                        timestamp = DateTime.UtcNow,
                        service = "logging-elk-api",
                        version = "1.0.0"
                    });
                });

                // Test logging endpoint
                app.MapGet("/test-log", () =>
                {
                    // Ví d? các m?c ?? log khác nhau
                    Log.Verbose("This is a verbose log");
                    Log.Debug("This is a debug log");
                    Log.Information("This is an information log");
                    Log.Warning("This is a warning log");
                    Log.Error("This is an error log");

                    // Log v?i structured data
                    var order = new { Id = 123, Amount = 99.99m, Customer = "John Doe" };
                    Log.Information("Processing order {@Order}", order);

                    // Log v?i exception
                    try
                    {
                        throw new InvalidOperationException("Test exception for logging");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "An error occurred while processing");
                    }

                    return Results.Ok(new { message = "Test logs generated" });
                });

                // Log thông tin startup
                Log.Information("Application started successfully");
                Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);
                Log.Information("Listening on: {Urls}", string.Join(", ", app.Urls));

                app.Run();
            }
            catch (Exception ex)
            {
                // Log l?i startup
                Log.Fatal(ex, "Application startup failed");
                throw;
            }
            finally
            {
                // ??m b?o log ???c flush tr??c khi ?ng d?ng d?ng
                Log.CloseAndFlush();
            }
        }
    }
}