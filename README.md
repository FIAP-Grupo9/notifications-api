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

## CI/CD

GitHub Actions (`.github/workflows/ci.yml`):

- **CI** — em todo push ou pull request: `dotnet restore` + `build` do `NotificationsAPI.sln`. Ainda não há projeto de testes neste serviço.
- **CD** — só em push na `master`: build e publicação da imagem Docker no **Azure Container Registry** (`$ACR_LOGIN_SERVER/notifications-api`, tags `latest` e `<sha>`), autenticando com o Service Principal do projeto.
- **Deploy** — na sequência, aplica `k8s/configmap.yaml`/`secret.yaml` e `k8s/deployment.yaml`/`service.yaml` com a imagem recém-publicada no **AKS**, confirmando rolling update sem downtime com `kubectl rollout status`. `k8s/secret.yaml` só guarda `guest`/`guest` (credencial padrão de desenvolvimento do próprio RabbitMQ, não um segredo de negócio), por isso não precisa de substituição via `envsubst` como nos demais serviços.

Secrets do GitHub usados por este workflow (nível de organização `FIAP-Grupo9`): `AZURE_CREDENTIALS`, `ACR_LOGIN_SERVER`, `AKS_RESOURCE_GROUP`, `AKS_CLUSTER_NAME`. A infraestrutura compartilhada (Postgres, RabbitMQ, MongoDB, Redis, Elasticsearch, Kong) é aplicada separadamente pelo workflow `deploy-infra.yml` do repositório `orchestration` — ver o README dele.

A imagem aplicada no `Deployment` é sempre a tag `<sha>` do commit (nunca `latest`): isso torna o rollout determinístico — o mesmo `kubectl apply` nunca reaplica uma imagem já em execução por engano, e dá pra rastrear exatamente qual commit está rodando no cluster a qualquer momento.

## Decisões arquiteturais

- **Stateless** — sem banco de dados. Em produção, considerar log de envios para auditoria/idempotência cross-restart.
- **Sem deduplicação por `MessageId` ainda** — para eventos de notificação, pequena duplicação é aceitável (no pior caso o usuário recebe 2 e-mails). Em fluxos críticos (pagamentos), o `MessageId` é usado em CatalogAPI/PaymentsAPI.
- **Sem retries customizados além do default do MassTransit** — política global em `FCG.BuildingBlocks/MessagingExtensions.cs` (3 retries com backoff: 2s, 5s, 15s).


