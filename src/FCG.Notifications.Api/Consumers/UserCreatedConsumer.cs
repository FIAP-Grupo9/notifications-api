using FCG.Contracts.Events;
using MassTransit;

namespace FCG.Notifications.Api.Consumers;

public class UserCreatedConsumer : IConsumer<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedConsumer> _logger;

    public UserCreatedConsumer(ILogger<UserCreatedConsumer> logger) => _logger = logger;

    public Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        var e = context.Message;
        // "Envio" do e-mail é simulado via log no console (requisito do PDF).
        _logger.LogInformation(
            "[EMAIL] Bem-vindo(a), {Name}! Confirmação de cadastro enviada para {Email} (UserId={UserId}, MessageId={MessageId})",
            e.Name, e.Email, e.UserId, e.MessageId);
        return Task.CompletedTask;
    }
}
