# StreamPulse — Roadmap de Mejoras

Este documento registra las mejoras planificadas post-refactor inicial (corto plazo completado el 2026-04-29).  
Está organizado por horizonte de impacto, con justificación técnica y criterio de done para cada ítem.

---

## Completado — Corto plazo (2026-04-29)

- [x] Eliminar `UnitTest1.cs` (placeholder vacío)
- [x] `nginx.conf` verificado y funcional (SPA routing + proxy SignalR)
- [x] CORS extraído a `appsettings.json` (`Cors:AllowedOrigins`)
- [x] Parsing manual de JSON en `TransactionConsumerWorker` reemplazado por `TransactionMessage` DTO
- [x] Warmup estadístico en `AnomalyDetector` (`MinSamplesForDetection = 30`)
- [x] `GetAmountStats()` retorna `(Mean, StdDev, Count)` 
- [x] 9 tests nuevos para `TransactionAggregator` (percentiles, Flush, SuccessRate, FailureReasons)
- [x] 3 tests nuevos para `AnomalyDetector` (warmup, stdDev=0, cuentas independientes)

---

## Mediano plazo

### 1. Dead Letter Queue — Implementar consumer

**Estado:** El topic `transactions.dlq` está declarado en Kafka pero nadie lo consume.  
**Problema:** Los mensajes que fallan deserialización o procesamiento se pierden silenciosamente. En fintech eso es un riesgo de auditoría.

**Qué hacer:**
- Crear `DlqConsumerWorker` en el Processor que consuma `transactions.dlq`
- Cuando el `ProcessLoop` falle deserialización, publicar el mensaje original a `transactions.dlq` con metadata del error (timestamp, excepción, worker ID)
- Loguear cada mensaje DLQ con nivel `Error` e incluir el payload original

**Criterio de done:** Un mensaje malformado en `transactions.raw` aparece en `transactions.dlq` con su contexto de error dentro de los 5 segundos.

---

### 2. Observabilidad — OpenTelemetry

**Estado:** Ningún traceId viaja entre Producer → Kafka → Processor → Redis → API. Debuggear en producción es ciego.  
**Problema:** Si una transacción anómala llega al dashboard, no hay forma de rastrear qué pasó en cada etapa ni cuánto tardó cada salto.

**Qué hacer:**
- Agregar `OpenTelemetry.Exporter.Jaeger` (o Zipkin) a los tres servicios .NET
- Propagar `TraceId` como header Kafka en el Producer (`W3C TraceContext` sobre el header `traceparent`)
- Extraer el contexto en el Processor al consumir
- Instrumentar: `KafkaProducerService.PublishAsync`, `TransactionConsumerWorker.ProcessLoop`, `RedisMetricsService`, `MetricsController`
- Agregar Jaeger al `docker-compose.yml`

**Criterio de done:** Una transacción puede rastrearse end-to-end desde el Producer hasta la API en la UI de Jaeger.

---

### 3. Schema Registry — Contrato versionado

**Estado:** El contrato entre Producer y Processor es JSON implícito sin versión. Un cambio en `TransactionEvent` rompe silenciosamente el Processor.  
**Problema actual:** Si se agrega un campo nuevo con `[JsonRequired]` en el Producer, el Processor falla en deserialización sin ninguna alerta preventiva.

**Qué hacer:**
- Agregar Confluent Schema Registry al `docker-compose.yml`
- Migrar serialización a **Avro** con `Confluent.SchemaRegistry.Serdes.Avro`
- Definir `transaction_event.avsc` como fuente de verdad del contrato
- Configurar compatibilidad `BACKWARD` en el Schema Registry

**Criterio de done:** Intentar publicar con un schema incompatible falla en el Producer, no en el Processor.

---

### 4. Redis Streams — Eliminar polling en la API

**Estado:** `MetricsBroadcastService` hace polling a Redis cada 2 segundos (`StringGetAsync`) y transmite a todos los clientes SignalR aunque no haya datos nuevos.  
**Problema:** Si el Processor flushea cada 60 segundos, la API hace 30 lecturas innecesarias entre cada actualización real.

**Qué hacer:**
- Cambiar `RedisMetricsService.SaveMetricsAsync` para publicar en un Redis Stream (`XADD streampulse:stream *`)
- Cambiar `MetricsBroadcastService` para leer del stream con `XREAD BLOCK 0` (blocking read) en vez del timer de 2 segundos
- El broadcast a SignalR sucede solo cuando hay un mensaje nuevo en el stream

**Criterio de done:** El broadcast a SignalR se dispara exactamente cuando el Processor hace flush, no antes ni 30 veces en el medio.

---

### 5. Prometheus + Grafana — Métricas de infraestructura

**Estado:** El proyecto calcula métricas de negocio (P90/P95/P99, success rate) pero no expone métricas de infraestructura (lag de Kafka, uso de memoria, errores por segundo).

**Qué hacer:**
- Agregar `prometheus-net.AspNetCore` a la API (`/metrics` endpoint)
- Agregar `prometheus-net` al Processor y Producer (con `MetricServer` standalone)
- Métricas clave a exponer:
  - `kafka_consumer_lag` (mensajes sin procesar en el topic)
  - `transaction_processing_duration_seconds` (histogram)
  - `anomaly_detected_total` (counter)
  - `buffer_size` (gauge del Channel bounded)
- Agregar Prometheus + Grafana al `docker-compose.yml` con un dashboard pre-configurado

**Criterio de done:** Un dashboard Grafana muestra lag de Kafka, tasa de anomalías y latencia P99 con datos en tiempo real.

---

## Largo plazo

### 6. Persistencia histórica — PostgreSQL o ClickHouse

**Estado:** Redis es el único almacenamiento de métricas, con retención de 1 hora (60 ventanas × 60 segundos). Después se pierden.  
**Impacto regulatorio:** BCRA exige retención de logs de transacciones por 5 años. Ningún sistema de cumplimiento puede operar solo sobre Redis.

**Qué hacer:**
- **Opción A (simple):** PostgreSQL con tabla `window_metrics(window_start, total, completed, failed, p90_ms, p95_ms, p99_ms, anomaly_count, failure_reasons jsonb)`
- **Opción B (analítica):** ClickHouse para queries OLAP sobre millones de ventanas históricas
- El `FlushLoop` del Processor escribe a Redis Y a la base elegida
- La API agrega endpoint `GET /api/metrics/history?from=&to=` para queries históricas

**Criterio de done:** Las métricas de los últimos 30 días son consultables via API con filtros de fecha.

---

### 7. Autenticación — JWT Bearer en la API

**Estado:** La API es completamente pública. Cualquiera con acceso a `localhost:5000` puede leer métricas y conectarse al hub SignalR.

**Qué hacer:**
- Agregar `Microsoft.AspNetCore.Authentication.JwtBearer`
- Proteger `MetricsController` y `MetricsHub` con `[Authorize]`
- Para el hub SignalR, leer el token del query string (el cliente no puede enviar headers en WebSocket)
- Para demo/dev, agregar un endpoint `POST /api/auth/token` con credenciales fijas en config

**Criterio de done:** Un request sin token a `/api/metrics/latest` retorna 401. El dashboard incluye el token en la conexión SignalR.

---

### 8. Kubernetes — Manifests de producción

**Estado:** El deploy es solo docker-compose. No hay liveness/readiness probes, no hay auto-scaling, no hay gestión de secretos.

**Qué hacer:**
- Crear `k8s/` con Deployments, Services y ConfigMaps para cada servicio
- Liveness probe: `GET /api/metrics/health` (ya existe en la API)
- Readiness probe: verificar conectividad a Redis antes de aceptar tráfico
- HorizontalPodAutoscaler para el Processor (escalar basado en lag de Kafka)
- Secretos (Redis password, JWT secret) en Kubernetes Secrets, no en ConfigMaps
- Reemplazar docker-compose.override.yml con un `values.yaml` de Helm

**Criterio de done:** `kubectl apply -f k8s/` levanta el stack completo con health checks funcionando y el Processor escala automáticamente cuando el lag supera 1000 mensajes.

---

### 9. Circuit Breakers y Resiliencia

**Estado:** Si Redis cae, el Processor falla sin reintentos y la API devuelve 500 en todos los endpoints. No hay degradación graceful.

**Qué hacer:**
- Agregar `Polly` (o `Microsoft.Extensions.Http.Resilience`)
- Circuit breaker en `RedisMetricsService`: si Redis falla 3 veces seguidas, abrir el circuito por 30 segundos y loguear (no crashear el servicio)
- Retry con backoff exponencial en `KafkaProducerService` para errores transitorios de broker
- Fallback en la API: si Redis no responde, devolver el último valor cacheado en memoria (in-process cache con `IMemoryCache`)

**Criterio de done:** Apagar Redis con el sistema corriendo no crashea ningún servicio. La API devuelve datos (posiblemente stale) durante la interrupción.

---

## Prioridad sugerida

| Prioridad | Ítem | Justificación |
|-----------|------|---------------|
| Alta | OpenTelemetry (#2) | Bloquea debuggeo en producción |
| Alta | DLQ consumer (#1) | Riesgo de pérdida silenciosa de datos |
| Media | Prometheus + Grafana (#5) | Visibility de infraestructura |
| Media | Redis Streams (#4) | Eficiencia operacional |
| Media | Autenticación (#7) | Necesario antes de cualquier deploy público |
| Baja | Schema Registry (#3) | Útil cuando el modelo de transacción evolucione |
| Baja | PostgreSQL (#6) | Necesario para cumplimiento regulatorio |
| Baja | Kubernetes (#8) | Cuando el deploy a producción sea inminente |
| Baja | Circuit Breakers (#9) | Junto con Kubernetes |
