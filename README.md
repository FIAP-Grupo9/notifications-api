> **Nota:** Esta e a copia independente do microsservico **NotificationsAPI** gerada por split-repos.ps1 a partir do monorepo TechChallenge-Fiap-FCG. Shared/ foi embedded em ./shared/. Build local: docker build -t fcg-notifications-api . (da raiz deste repo).

---

# NotificationsAPI â€” FCG MicrosserviÃ§o de NotificaÃ§Ãµes

MicrosserviÃ§o **consumer-only**. Escuta eventos do RabbitMQ e "envia" notificaÃ§Ãµes por e-mail (simulado via log no console, conforme requisito do PDF da Fase 2).

---

## Responsabilidades

- Consumir `UserCreatedEvent` â†’ log de boas-vindas
- Consumir `PaymentProcessedEvent` â†’ log de confirmaÃ§Ã£o (Approved) ou de falha (Rejected)

## NÃ£o Ã© responsabilidade

- Expor endpoints REST de negÃ³cio (apenas `/health` e `/`)
- Persistir mensagens enviadas (stateless â€” replay via redelivery do broker)

---

## Eventos consumidos

| Evento | Origem | AÃ§Ã£o |
|---|---|---|
| `UserCreatedEvent` | UsersAPI | Log: `[EMAIL] Bem-vindo(a), {Name}!` |
| `PaymentProcessedEvent` (Approved) | PaymentsAPI | Log: `[EMAIL] Compra aprovada! '{GameName}' adicionado...` |
| `PaymentProcessedEvent` (Rejected) | PaymentsAPI | Log: `[EMAIL] Falha no pagamento...` |

## Eventos publicados

Nenhum.

---

## VariÃ¡veis de ambiente

| VariÃ¡vel | Default | DescriÃ§Ã£o |
|---|---|---|
| `RabbitMq__Host` | `localhost` | Host do broker |
| `RabbitMq__Port` | `5672` | Porta AMQP |
| `RabbitMq__Username` / `__Password` | `guest`/`guest` | Credenciais |

NÃ£o precisa de `ConnectionStrings__DefaultConnection` nem de `JwtSettings` â€” o serviÃ§o Ã© stateless e nÃ£o autentica usuÃ¡rios.

---

## Como rodar localmente

```powershell
docker compose -f ..\Orchestration\docker-compose.yml up -d rabbitmq
dotnet run --project src/FCG.Notifications.Api
```

Em outro terminal, dispare um cadastro no UsersAPI e veja o log:

```powershell
curl -X POST http://localhost:5001/api/auth/register `
  -H "Content-Type: application/json" `
  -d '{"name":"Teste","email":"teste@exemplo.com","password":"Senha@1234"}'

# No console da NotificationsAPI deve aparecer:
# [EMAIL] Bem-vindo(a), Teste! ConfirmaÃ§Ã£o de cadastro enviada para teste@exemplo.com ...
```

### Deploy K8s

```powershell
kubectl apply -f k8s/
kubectl logs -n fcg -l app=notifications-api -f
```

---

## DecisÃµes arquiteturais

- **Stateless** â€” sem banco de dados. Em produÃ§Ã£o, considerar log de envios para auditoria/idempotÃªncia cross-restart.
- **Sem deduplicaÃ§Ã£o por `MessageId` ainda** â€” para eventos de notificaÃ§Ã£o, pequena duplicaÃ§Ã£o Ã© aceitÃ¡vel (no pior caso o usuÃ¡rio recebe 2 e-mails). Em fluxos crÃ­ticos (pagamentos), o `MessageId` Ã© usado em CatalogAPI/PaymentsAPI.
- **Sem retries customizados alÃ©m do default do MassTransit** â€” polÃ­tica global em `FCG.BuildingBlocks/MessagingExtensions.cs` (3 retries com backoff: 2s, 5s, 15s).

