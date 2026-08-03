# Order Orchestration (Saga) — Bản cập nhật đúng với code hiện tại

## 1) Mục tiêu và phạm vi

Hệ thống đang dùng **MassTransit SagaStateMachine** để orchestration vòng đời đơn hàng:

`OrderCreated -> Validate -> Accept -> Complete -> Done/Rejected`

Mục tiêu hiện tại:

- Dùng **PostgreSQL transport** thay RabbitMQ cho message bus.
- Giữ cách gửi bằng URI `queue:...` trong activity (đây là cách đúng và chuẩn với MassTransit, không phụ thuộc Rabbit riêng).
- Saga state được lưu bằng **EF Core + PostgreSQL** để resume được sau crash.

---

## 2) Thành phần chính

### 2.1 Orchestrator

- Project: [OrderOrchestration/](D:/Univer/Nam_4/VB/Demo-Saga/OrderOrchestration)
- State machine: [OrderSagaStateMachine.cs](D:/Univer/Nam_4/VB/Demo-Saga/OrderOrchestration/OrderSagaStateMachine.cs)
- Activities: [OrderSagaActivities.cs](D:/Univer/Nam_4/VB/Demo-Saga/OrderOrchestration/Activities/OrderSagaActivities.cs)
- Transport config: [Program.cs](D:/Univer/Nam_4/VB/Demo-Saga/OrderOrchestration/Program.cs) (`UsingPostgres`)

### 2.2 Worker services (consume command, publish event)

- `OrderSubmitService`: xử lý `ValidateOrderCommand`
- `OrderAcceptService`: xử lý `AcceptOrderCommand`, `CancelOrderCommand`, timeout watcher
- `OrderCompleteService`: xử lý `CompleteOrderCommand`

### 2.3 Notification

- `NotificationService` consume các `*Response` để đẩy SignalR.

---

## 3) Transport & routing (đã chuẩn hóa PostgreSQL)

### 3.1 Transport

Tất cả service liên quan orchestration dùng `UsingPostgres(...)` với `SqlTransportOptions.ConnectionString = PostgresConnection`.

### 3.2 Routing bằng URI queue

Trong saga activity, cách gọi sau **đúng và nên giữ**:

- `new Uri("queue:order-validation-queue")`
- `new Uri("queue:order-accept-queue")`
- `new Uri("queue:order-complete-queue")`
- `new Uri("queue:order-cancel-queue")`

`queue:` là logical address của MassTransit endpoint, không khóa vào RabbitMQ; đổi transport sang PostgreSQL vẫn chạy đúng.

---

## 4) State machine thực tế (theo code hiện tại)

Tham chiếu: [OrderSagaStateMachine.cs](D:/Univer/Nam_4/VB/Demo-Saga/OrderOrchestration/OrderSagaStateMachine.cs)

## 4.1 States đang khai báo

- `Submitted` (đang khai báo nhưng **chưa dùng trong transition**)
- `Validating`
- `Accepting`
- `Completing`
- `Completed`
- `Rejected`

## 4.2 Correlation

Toàn bộ event đều `CorrelateById(m => m.Message.OrderId)`.

=> `OrderId` là correlation key xuyên suốt luồng.

## 4.3 Transition table (đúng với code)

| Current state | Incoming event | Activity xử lý | Outgoing action | Next state |
|---|---|---|---|---|
| `Initially` | `OrderCreatedEvent` | `OrderCreatedActivity` | Send `ValidateOrderCommand` -> `queue:order-validation-queue` | `Validating` |
| `Validating` | `OrderValidatedEvent` | `OrderValidatedActivity` | Send `AcceptOrderCommand` -> `queue:order-accept-queue` | `Accepting` |
| `Validating` | `OrderValidationFailedEvent` | `OrderValidationFailedActivity` | Publish `OrderSubmitFailedResponse` | `Rejected` |
| `Accepting` | `OrderAcceptedEvent` | `OrderAcceptedActivity` | Send `CompleteOrderCommand` -> `queue:order-complete-queue` | `Completing` |
| `Accepting` | `OrderAcceptFailedEvent` | `OrderAcceptFailedActivity` | Publish `OrderAcceptFailedResponse` | `Rejected` |
| `Completing` | `OrderCompletedEvent` | `OrderCompletedActivity` | Publish `OrderCompleteSuccessResponse` | `Completed` |
| `Completing` | `OrderCompleteFailedEvent` | `OrderCompleteFailedActivity` | Send `CancelOrderCommand` -> `queue:order-cancel-queue`, rồi publish `OrderCompleteFailedResponse` | `Rejected` |
| `DuringAny` | `OrderTimeoutExpiredEvent` | `OrderTimeoutExpiredActivity` | Send `CancelOrderCommand` -> `queue:order-cancel-queue`, rồi publish `OrderAcceptFailedResponse` (reason: Timeout) | `Rejected` |

---

## 5) Activity behavior chi tiết

Tham chiếu: [OrderSagaActivities.cs](D:/Univer/Nam_4/VB/Demo-Saga/OrderOrchestration/Activities/OrderSagaActivities.cs)

### 5.1 OrderCreatedActivity

- Gán dữ liệu vào saga instance:
  - `CorrelationId = OrderId`
  - `CustomerId`, `TotalAmount`, `Items`, `CreatedAt`
- Gửi `ValidateOrderCommand` sang `order-validation-queue`.

### 5.2 OrderValidatedActivity

- Gửi `AcceptOrderCommand` sang `order-accept-queue`.

### 5.3 OrderValidationFailedActivity

- Publish `OrderSubmitFailedResponse` (kèm `NotificationPayLoad`).

### 5.4 OrderAcceptedActivity

- Gửi `CompleteOrderCommand` sang `order-complete-queue`.
- Dùng `context.Saga.Items` để đảm bảo item theo state đã lưu.

### 5.5 OrderAcceptFailedActivity

- Publish `OrderAcceptFailedResponse` (kèm `NotificationPayLoad`).

### 5.6 OrderCompletedActivity

- Publish `OrderCompleteSuccessResponse` (kèm `NotificationPayLoad`).

### 5.7 OrderCompleteFailedActivity

- Gửi compensate `CancelOrderCommand` sang `order-cancel-queue`.
- Publish `OrderCompleteFailedResponse`.

### 5.8 OrderTimeoutExpiredActivity

- Gửi compensate `CancelOrderCommand` (reason timeout).
- Publish `OrderAcceptFailedResponse` với nội dung timeout cho UI.

---

## 6) Queue / endpoint map

| Queue | Producer | Consumer |
|---|---|---|
| `order-validation-queue` | Saga (`OrderCreatedActivity`) | `OrderSubmitService` |
| `order-accept-queue` | Saga (`OrderValidatedActivity`) | `OrderAcceptService` |
| `order-complete-queue` | Saga (`OrderAcceptedActivity`) | `OrderCompleteService` |
| `order-cancel-queue` | Saga (`OrderCompleteFailedActivity`, `OrderTimeoutExpiredActivity`) | `OrderAcceptService` (`CancelOrderConsumer`) |
| `notification-queue` | Các luồng publish `*Response` | `NotificationService` |

---

## 7) Luồng nghiệp vụ end-to-end

### 7.1 Happy path

1. Web/App publish `OrderCreatedEvent`
2. Saga -> send `ValidateOrderCommand`
3. Submit service publish `OrderValidatedEvent`
4. Saga -> send `AcceptOrderCommand`
5. Accept service publish `OrderAcceptedEvent`
6. Saga -> send `CompleteOrderCommand`
7. Complete service publish `OrderCompletedEvent`
8. Saga publish `OrderCompleteSuccessResponse`
9. NotificationService đẩy SignalR cho UI

### 7.2 Validation fail

1. Sau validate, submit service publish `OrderValidationFailedEvent`
2. Saga publish `OrderSubmitFailedResponse`
3. Saga chuyển `Rejected`

### 7.3 Accept fail

1. Accept service publish `OrderAcceptFailedEvent`
2. Saga publish `OrderAcceptFailedResponse`
3. Saga chuyển `Rejected`

### 7.4 Complete fail

1. Complete service publish `OrderCompleteFailedEvent`
2. Saga send `CancelOrderCommand`
3. Saga publish `OrderCompleteFailedResponse`
4. Saga chuyển `Rejected`

### 7.5 Timeout (DuringAny)

1. Timeout watcher publish `OrderTimeoutExpiredEvent`
2. Saga send `CancelOrderCommand`
3. Saga publish `OrderAcceptFailedResponse` (timeout)
4. Saga chuyển `Rejected`

---

## 8) Điểm cần lưu ý kỹ thuật

1. **`Submitted` đang không dùng**
   - State có khai báo nhưng không transition tới.
   - Nếu muốn dùng, cần đổi `Initially` thành `TransitionTo(Submitted)` rồi tách bước tiếp theo.

2. **Idempotency**
   - Vì distributed system có thể redelivery, các consumer command nên idempotent theo `OrderId`.

3. **Retry / outbox**
   - Nên bật retry policy và (nếu cần) outbox theo từng service để tránh duplicate side-effect.

4. **Schema Postgres transport**
   - `cfg.AutoStart = true` giúp auto-init transport objects.
   - Môi trường production nên kiểm soát migration/DDL rõ ràng.

5. **Observability**
   - Log phải luôn chứa `OrderId` (correlation id) để trace full flow.

---

## 9) Checklist xác nhận sau khi đổi Rabbit -> PostgreSQL transport

- [ ] Không còn `UsingRabbitMq` trong các service orchestration.
- [ ] `OrderOrchestration` và `NotificationService` đã có `UsingPostgres`.
- [ ] `SqlTransportOptions.ConnectionString` map đúng `PostgresConnection`.
- [ ] Queue URI `queue:...` giữ nguyên (không cần đổi).
- [ ] Build thành công các project liên quan.
- [ ] Test đủ 5 luồng: happy / validation fail / accept fail / complete fail / timeout.

---

## 10) Kết luận

Thiết kế hiện tại đã đúng hướng doanh nghiệp:

- Orchestration tập trung bằng Saga state machine.
- Compensation rõ ràng (`CancelOrderCommand`).
- Transport đã chuyển về PostgreSQL và vẫn dùng tốt `queue:` URI chuẩn của MassTransit.
- Dễ mở rộng retry, observability và kiểm soát trạng thái toàn cục theo `OrderId`.
