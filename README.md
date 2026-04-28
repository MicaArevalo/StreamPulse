# StreamPulse

![CI](https://github.com/TU_USUARIO/StreamPulse/actions/workflows/ci.yml/badge.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![Kafka](https://img.shields.io/badge/Apache_Kafka-231F20?logo=apachekafka&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-DC382D?logo=redis&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?logo=docker&logoColor=white)

Pipeline de analytics fintech en tiempo real. Procesa miles de eventos de transacciones por segundo, detecta anomalías y las visualiza en un dashboard live.

## Arquitectura

```
┌─────────────┐    ┌─────────────────────────────────┐    ┌──────────┐
│  Producer   │───▶│              Kafka               │───▶│Processor │
│  (Worker)   │    │  transactions.raw  (3 partitions)│    │ (Worker) │
└─────────────┘    │  transactions.completed          │    └────┬─────┘
                   │  transactions.failed             │         │
                   │  transactions.dlq                │         ▼
                   └─────────────────────────────────┘    ┌──────────┐
                                                           │  Redis   │
                                                           └────┬─────┘
                                                                │
                                                      ┌─────────┴─────────┐
                                                      │                   │
                                                 ┌────▼─────┐    ┌────────▼──────┐
                                                 │   API    │───▶│   Dashboard   │
                                                 │ SignalR  │    │  React + Vite │
                                                 └──────────┘    └───────────────┘
```

## Features

- **Kafka producers/consumers** con idempotencia (`EnableIdempotence=true`, `Acks.All`), commit manual de offsets
- **Backpressure handling** con `Channel` bounded buffer (500 mensajes)
- **Agregación en ventanas** de 1 minuto con métricas P90/P95/P99
- **Detección de anomalías** por desvío estándar (3σ) y velocidad por cuenta (>30 tx/60s)
- **Dead Letter Queue topic** definido en la arquitectura, listo para conectar un consumer de reintentos
- **Dashboard en vivo** via SignalR — se actualiza cada 2 segundos
- **Testcontainers** — tests de integración con Kafka real, sin mocks

## Stack

| Capa | Tecnología |
|---|---|
| Backend | .NET 8 · ASP.NET Core · C# |
| Streaming | Apache Kafka (Confluent SDK 2.3) |
| Cache | Redis (StackExchange.Redis) |
| Real-time | SignalR |
| Frontend | React · Vite · TypeScript · Recharts |
| Tests | xUnit · Testcontainers · FluentAssertions |
| Infra | Docker · Docker Compose |
| CI/CD | GitHub Actions |

## Levantar el entorno completo

**Requisitos:** Docker Desktop, .NET 8 SDK

```bash
# Clonar
git clone https://github.com/TU_USUARIO/StreamPulse.git
cd StreamPulse

# Levantar todo con un comando
docker-compose -f docker-compose.yml -f docker-compose.override.yml up --build
```

| Servicio | URL |
|---|---|
| Dashboard | http://localhost:3000 |
| API REST | http://localhost:5000 |
| Kafka UI | http://localhost:8080 |

## Desarrollo local

```bash
# Solo infraestructura
docker-compose up -d zookeeper kafka kafka-ui redis

# Servicios .NET (terminales separadas)
dotnet run --project src/StreamPulse.Producer
dotnet run --project src/StreamPulse.Processor
dotnet run --project src/StreamPulse.Api

# Dashboard
cd dashboard
npm run dev   # http://localhost:3000
```

## Tests

```bash
# Tests unitarios (sin Docker)
dotnet test --filter "Category!=Integration"

# Todos los tests (requiere Docker corriendo)
dotnet test
```

## Métricas del dashboard

| Métrica | Descripción |
|---|---|
| Transacciones / ventana | Volumen procesado en la ventana de 60s |
| Tasa de éxito % | Completadas vs total |
| Volumen $ | Suma de montos en la ventana |
| Latencia promedio | ProcessingTimeMs promedio |
| P90 / P95 / P99 | Percentiles de latencia |
| Anomalías activas | Detectadas por 3σ o velocity fraud |
| Distribución de errores | INSUFFICIENT_FUNDS / TIMEOUT / FRAUD_DETECTED |

## Decisiones de arquitectura

**¿Por qué tres servicios separados?**
Producer, Processor y Api tienen responsabilidades y ciclos de vida distintos. El Processor puede escalar horizontalmente — Kafka consumer groups distribuye las particiones automáticamente entre instancias sin cambiar código.

**¿Por qué backpressure con Channel?**
Si el procesamiento no puede seguir el ritmo de Kafka, el buffer bounded pausa el poll en lugar de perder mensajes o acumular indefinidamente en memoria.

**¿Por qué Testcontainers en lugar de mocks?**
Mockear Kafka da falsa confianza. Testcontainers levanta un broker real en Docker — los tests prueban el comportamiento real del sistema, incluyendo serialización, particionado y commit de offsets.

**¿Por qué el dashboard está desacoplado?**
El frontend no tiene ninguna referencia al código .NET. Se comunica exclusivamente via HTTP REST y WebSocket (SignalR). Puede reemplazarse por cualquier otro cliente sin modificar el backend.

## Conexión con PaymentHub (P3)

StreamPulse puede consumir eventos reales de PaymentHub apuntando al mismo broker Kafka. Los eventos `PaymentCompleted` y `PaymentFailed` aparecen automáticamente en el dashboard sin modificar código.
