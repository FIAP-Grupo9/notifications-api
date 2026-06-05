using FCG.Contracts.Events;
using MassTransit;

namespace FCG.Notifications.Api.Consumers;

public class PaymentProcessedConsumer : IConsumer<PaymentProcessedEvent>
{
    private readonly ILogger<PaymentProcessedConsumer> _logger;

    public PaymentProcessedConsumer(ILogger<PaymentProcessedConsumer> logger) => _logger = logger;

    public Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var e = context.Message;

        if (e.Status == PaymentStatus.Approved)
        {
            _logger.LogInformation(
                "[EMAIL] Compra aprovada! '{GameName}' adicionado à sua biblioteca. Recibo enviado para {Email} (OrderId={OrderId}, Amount={Amount:0.00})",
                e.GameName, e.UserEmail, e.OrderId, e.Amount);
        }
        else
        {
            _logger.LogInformation(
                "[EMAIL] Falha no pagamento de '{GameName}'. Motivo: {Reason}. Nota enviada para {Email} (OrderId={OrderId})",
                e.GameName, e.FailureReason ?? "Não informado", e.UserEmail, e.OrderId);
        }

        return Task.CompletedTask;
    }
}
