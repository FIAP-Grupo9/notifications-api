using FCG.BuildingBlocks.Messaging;
using FCG.BuildingBlocks.Middleware;
using FCG.Notifications.Api.Consumers;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    builder.Services.AddFcgMessaging(builder.Configuration, x =>
    {
        x.AddConsumer<UserCreatedConsumer>();
        x.AddConsumer<PaymentProcessedConsumer>();
    });

    var app = builder.Build();

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();
    app.MapGet("/", () => Results.Ok(new { service = "NotificationsAPI", consumers = new[] { "UserCreatedEvent", "PaymentProcessedEvent" } }));
    app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "NotificationsAPI" }));

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "NotificationsAPI encerrada inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}
