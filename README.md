> **⚠️ Repositório descontinuado.** Na Fase 3 do Tech Challenge, este serviço foi
> substituído pela função serverless **`fcg-notifications-fn`**, que faz o mesmo
> trabalho mas só existe enquanto há mensagem para processar — em vez de manter um
> contêiner ligado 24 horas por dia aguardando eventos. O código aqui permanece
> como referência histórica da Fase 2.

---
> **Nota:** Esta e a copia independente do microsservico **NotificationsAPI** gerada por split-repos.ps1 a partir do monorepo TechChallenge-Fiap-FCG. Shared/ foi embedded em ./shared/. Build local: docker build -t fcg-notifications-api . (da raiz deste repo).

---

# NotificationsAPI — FCG Microsserviço de Notificações

Microsserviço **consumer-only**. Escuta eventos do RabbitMQ e "envia" notificações por e-mail (simulado via log no console, conforme requisito do PDF da Fase 2).

---

## Responsabilidades

- Consumir `UserCreatedEvent` → log de boas-vindas
- Consumir `PaymentProcessedEvent` → log de confirmação (Approved) ou de falha (Rejected)

## Não é responsabilidade

- Expor endpoints REST de negócio (apenas `/health` e `/`)
- Persistir mensagens enviadas (stateless — replay via redelivery do broker)

---

## Eventos consumidos

| Evento | Origem | Ação |
|---|---|---|
| `UserCreatedEvent` | UsersAPI | Log: `[EMAIL] Bem-vindo(a), {Name}!` |
| `PaymentProcessedEvent` (Approved) | PaymentsAPI | Log: `[EMAIL] Compra aprovada! '{GameName}' adicionado...` |
| `PaymentProcessedEvent` (Rejected) | PaymentsAPI | Log: `[EMAIL] Falha no pagamento...` |

## Eventos publicados

Nenhum.

---

## Variáveis de ambiente

| Variável | Default | Descrição |
|---|---|---|
| `RabbitMq__Host` | `localhost` | Host do broker |
| `RabbitMq__Port` | `5672` | Porta AMQP |
| `RabbitMq__Username` / `__Password` | `guest`/`guest` | Credenciais |

Não precisa de `ConnectionStrings__DefaultConnection` nem de `JwtSettings` — o serviço é stateless e não autentica usuários.

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
# [EMAIL] Bem-vindo(a), Teste! Confirmação de cadastro enviada para teste@exemplo.com ...
```

### Deploy K8s

```powershell
kubectl apply -f k8s/
kubectl logs -n fcg -l app=notifications-api -f
```

---

## Decisões arquiteturais

- **Stateless** — sem banco de dados. Em produção, considerar log de envios para auditoria/idempotência cross-restart.
- **Sem deduplicação por `MessageId` ainda** — para eventos de notificação, pequena duplicação é aceitável (no pior caso o usuário recebe 2 e-mails). Em fluxos críticos (pagamentos), o `MessageId` é usado em CatalogAPI/PaymentsAPI.
- **Sem retries customizados além do default do MassTransit** — política global em `FCG.BuildingBlocks/MessagingExtensions.cs` (3 retries com backoff: 2s, 5s, 15s).


