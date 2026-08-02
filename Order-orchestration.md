# TÀI LIỆU CHUYỂN ĐỔI HỆ THỐNG XỬ LÝ ĐƠN HÀNG SANG ORCHESTRATION
## SAGA PATTERN (ORCHESTRATION) + MASSTRANSIT SAGASTATEMACHINE

---

## 1. TỔNG QUAN

### 1.1. Mục tiêu
Chuyển hệ thống hiện tại từ **Choreography** (mỗi consumer tự publish bước tiếp) sang **Orchestration** (1 orchestrator trung tâm quyết định thứ tự, gửi command, nhận response, xử lý compensate). Orchestrator được triển khai bằng **MassTransit SagaStateMachine** lưu saga instance xuống PostgreSQL để có thể resume sau crash.

### 1.2. Nguyên tắc cốt lõi thay đổi
| | Choreography (cũ) | Orchestration (mới) |
|:--|:-------------------|:---------------------|
| Consumer làm gì | Làm việc rồi **tự publish** bước tiếp | Chỉ **thi hành lệnh** (consume Command) rồi **báo kết quả** (publish Response) |
| Ai chọn bước kế | Từng consumer | `OrderSagaStateMachine` |
| Auto-timeout | Worker quét + consumer tự xử lý | Worker quét → báo saga → saga xử lý `DuringAny` |
| Trạng thái quy trình | Cột `Order.Status` | Saga instance `OrderState` + `Order.Status` |
| Notification UI | Từng consumer publish `*Response` | Saga publish `*Response` |

---

## 2. KIẾN TRÚC ĐÍCH

```
[WebApp] --OrderCreatedEvent--> [OrderOrchestratorService] (MỚI)
                                 OrderSagaStateMachine<OrderState>
                                 │ Send Command / Receive Response
       ┌─────────────────────────┼──────────────────────────┐
       ▼                         ▼                          ▼
[OrderSubmitService]      [OrderAcceptService]       [OrderCompleteService]
 ValidateOrderCommand     AcceptOrderCommand          CompleteOrderCommand
 → OrderValidatedEvent    → OrderAcceptedEvent        → OrderCompletedEvent
 (ValidationFailedEvent)  (AcceptFailedEvent)         (CompleteFailedEvent)
                              ▲    │
                              │    └── CancelOrderCommand (compensate) ──┘
       └───────────────────────┴──────────────────────────┘
                              ▼  (saga publish các *Response)
                   [NotificationService] (GIỮ NGUYÊN) → SignalR → UI
```

---

## 3. SERVICE MAP (CHUYỂN ĐỔI TỪNG SERVICE)

| Service | Consumer cũ | Chuyển thành | Đầu vào | Đầu ra |
|:--------|:-------------|:-------------|:--------|:-------|
| **OrderOrchestratorService** | — (mới) | `OrderSagaStateMachine` + `OrderState` | `OrderCreatedEvent` | Command + `*Response` |
| **OrderSubmitService** | `OrderSubmitConsumer : IConsumer<OrderSubmittedEvent>` | `IConsumer<ValidateOrderCommand>` | `ValidateOrderCommand` | `OrderValidatedEvent` / `OrderValidationFailedEvent` |
| **OrderAcceptService** | `OrderAcceptConsumer : IConsumer<ProcessOrderAcceptCommand>` | `IConsumer<AcceptOrderCommand>` | `AcceptOrderCommand` | `OrderAcceptedEvent` / `OrderAcceptFailedEvent` |
| **OrderAcceptService** | `OrderTimeoutConsumer` | **Xóa** (saga xử lý timeout) | — | — |
| **OrderAcceptService** | — | **Thêm** `CancelOrderConsumer : IConsumer<CancelOrderCommand>` | `CancelOrderCommand` (compensate) | `OrderCancelledEvent` |
| **OrderCompleteService** | `OrderCompleteConsumer : IConsumer<OrderCompleteEvent>` | `IConsumer<CompleteOrderCommand>` | `CompleteOrderCommand` | `OrderCompletedEvent` / `OrderCompleteFailedEvent` |
| **NotificationService** | — | **Giữ nguyên** | các `*Response` | SignalR → UI |

---

## 4. SAGA STATE MACHINE

### 4.1. States
`Submitted → Validating → Accepting → Completing → Completed` / `Rejected` (final)

### 4.2. Sơ đồ
```
 OrderCreated ──► Submitted ──(Send Validate)──► Validating
                                                    │
                               Validated ──────────┘   ValidationFailed ──► Rejected
                                                    ▼
                                                  Accepting
                               Accepted ───────────┘   AcceptFailed ────► Rejected
                                                    ▼
                                                  Completing
                               Completed ──────────┘   CompleteFailed ──► Compensate ──► Rejected
                                                    ▼
                                                 Completed
      (DuringAny: OrderTimeoutExpired ──► Compensate ──► Rejected)
```

### 4.3. Bảng transition
| State | Event đến | Saga gửi / làm | Transition tới |
|:------|:-----------|:----------------|:---------------|
| Submitted | OrderCreatedEvent | Send `ValidateOrderCommand` | Validating |
| Validating | OrderValidatedEvent | Send `AcceptOrderCommand` | Accepting |
| | OrderValidationFailedEvent | Publish `OrderSubmitFailedResponse` | Rejected |
| Accepting | OrderAcceptedEvent | Send `CompleteOrderCommand` | Completing |
| | OrderAcceptFailedEvent | Publish `OrderAcceptFailedResponse` | Rejected |
| Completing | OrderCompletedEvent | Publish `OrderCompleteSuccessResponse` | Completed |
| | OrderCompleteFailedEvent | Send `CancelOrderCommand` + Publish `OrderCompleteFailedResponse` | Rejected |
| Any (DuringAny) | OrderTimeoutExpiredEvent | Send `CancelOrderCommand` + Publish noti | Rejected |

> Tất cả event `CorrelateById(x => x.Message.OrderId)`; `InstanceState(x => x.CurrentState)`.

---

## 5. EVENT / COMMAND CATALOG

### 5.1. Thêm mới (Domain)
| Event | Dữ liệu |
|:------|:--------|
| **OrderCreatedEvent** | EventId, OrderId, CustomerId, Items, TotalAmount, Timestamp |
| **ValidateOrderCommand** | EventId, OrderId, CustomerId, Items, Timestamp |
| **AcceptOrderCommand** | EventId, OrderId, CustomerId, Timestamp |
| **CompleteOrderCommand** | EventId, OrderId, CustomerId, Items, Timestamp |
| **CancelOrderCommand** | EventId, OrderId, CustomerId, Reason, Timestamp |
| **OrderValidatedEvent** | EventId, OrderId, CustomerId, Items, Timestamp |
| **OrderValidationFailedEvent** | EventId, OrderId, CustomerId, ErrorMessage, Timestamp |
| **OrderCompletedEvent** | EventId, OrderId, CustomerId, Timestamp |
| **OrderCompleteFailedEvent** | EventId, OrderId, CustomerId, ErrorReason, Timestamp |
| **OrderCancelledEvent** | EventId, OrderId, CustomerId, Reason, Timestamp |
| **OrderTimeoutExpiredEvent** | CorrelationId, OrderId, Timestamp |

### 5.2. Giữ nguyên (saga dùng báo UI)
`OrderSubmitSuccessResponse`, `OrderSubmitFailedResponse`, `OrderAcceptSuccessResponse`, `OrderAcceptFailedResponse`, `OrderCompleteSuccessResponse`, `OrderCompleteFailedResponse`, `OrderAcceptedEvent`, `OrderAcceptFailedEvent`, `NotificationPayLoad`.

### 5.3. Ngừng dùng (giữ file, đánh dấu obsolete)
`OrderSubmittedEvent`, `ProcessOrderAcceptCommand`, `OrderCompleteEvent`, `OrderAutoTimeoutExpiredEvent`.

---

## 6. SAGA REPOSITORY — `OrderState`

- Package `MassTransit.EntityFrameworkCore` **9.2.0** (khớp các service).
- Entity `OrderState : SagaStateMachineInstance`: `CorrelationId` (=OrderId, PK), `CurrentState`, `CustomerId`, `TotalAmount`, `CreatedAt`.
- `OrderStateMap : SagaClassMap<OrderState>` → table `OrderState`, `CurrentState` max 64.
- DbContext `OrderSagaDbContext` (Npgsql) + `DesignTimeDbContextFactory` → tạo migration + `dotnet ef database update`.

---

## 7. THAY ĐỔI THEO TỪNG FILE

| File | Thay đổi |
|:-----|:---------|
| `Onion.CleanArchitecture.Domain/Events/*` | Thêm 11 event mới (5.1); obsolete 4 event cũ |
| `Onion.CleanArchitecture.Domain/Entities/OrderState.cs` | Mới |
| `Onion.CleanArchitecture.Infrastructure.Persistence/Migrations/*` | Migration `OrderState` |
| `OrderOrchestratorService/**` (mới) | Program.cs + `OrderSagaStateMachine.cs` + `OrderStateMap.cs` + `OrderSagaDbContext.cs` + `SystemUserService.cs` + appsettings.json |
| `OrderSubmitService/OrderSubmitConsumer.cs` | Đổi message type, bỏ publish bước tiếp |
| `OrderSubmitService/Program.cs` | Endpoint `order-validation-queue` |
| `OrderAcceptService/OrderAcceptConsumer.cs` | Đổi message type, bỏ publish `OrderCompleteEvent` |
| `OrderAcceptService/OrderTimeoutConsumer.cs` | Xóa |
| `OrderAcceptService/TimerWatcherBackgroundService.cs` | Đổi publish → `OrderTimeoutExpiredEvent` |
| `OrderAcceptService/Services/CancelOrderConsumer.cs` | Mới (compensate) |
| `OrderAcceptService/Program.cs` | Bỏ `OrderTimeoutConsumer`, thêm `CancelOrderConsumer` |
| `OrderCompleteService/OrderCompleteConsumer.cs` | Đổi message type, bỏ publish `*Response` |
| `OrderCompleteService/ShippingFailedConsumer.cs` | Xóa (file rỗng) |
| `OrderCompleteService/Program.cs` | Endpoint `order-complete-queue` |
| `Onion.CleanArchitecture.WebApp.Server/Controllers/v1/OrdersController.cs` | Publish `OrderCreatedEvent` |

---

## 8. COMPENSATION (BỒI HOÀN)

| Bước đã thành công | Khi bước kế fail | Compensation |
|:--------------------|:------------------|:-------------|
| Validate | Validation fail | Không cần (chưa side-effect) |
| Accept (`Accepted` + tạo timer) | Complete fail / Timeout | `CancelOrderCommand` → set `Rejected` + `RejectedAt` + hủy timer pending |
| Complete (trừ kho) | Không có bước sau | Không cần |

---

## 9. AUTO-TIMEOUT (SAU CHUYỂN ĐỔI)

- `TimerWatcherBackgroundService` **giữ ở OrderAcceptService**: quét timer `Pending & hết hạn`, publish `OrderTimeoutExpiredEvent`.
- Saga `DuringAny(When(OrderTimeoutExpiredEvent))` → gửi `CancelOrderCommand` (compensate) + publish noti → `Rejected`.

---

## 10. THỨ TỰ THỰC HIỆN

1. Domain: thêm event mới + obsolete event cũ
2. `OrderState` + `OrderSagaDbContext` + migration (`dotnet ef database update`)
3. Tạo `OrderOrchestratorService` (project mới, thêm vào .sln)
4. Sửa 3 consumer + Program.cs + thêm `CancelOrderConsumer`; xóa `OrderTimeoutConsumer` + `ShippingFailedConsumer`
5. Sửa WebApp publish `OrderCreatedEvent`
6. Build toàn solution
7. Test 3 kịch bản: thành công / validation fail / auto-timeout reject

---

## 11. RỦI RO & CẠM BẪY

1. **MassTransit version**: saga repo phải 9.2.0 (tránh NU1605 với 7.3.0 transitive từ Application).
2. **Correlation**: mọi event phải có `OrderId` + `CorrelateById`; thiếu → saga không match instance.
3. **Migration saga table**: phải update DB trước khi start orchestrator.
4. **Message tăng gấp đôi**: mỗi bước = command + response (chấp nhận, bản chất orchestration).
5. **`Order.CustomerId` là string** nhưng event dùng `Guid` → khi saga publish noti phải `Guid.Parse`.

---

## 12. ĐỐI CHIẾU SAU KHI CHUYỂN ĐỔI

| Đặc điểm | Trước (Choreography) | Sau (Orchestration) |
|:---------|:---------------------|:---------------------|
| Số service | 3 + Notification | 4 + Notification |
| Ai quyết định bước kế | Từng consumer | `OrderSagaStateMachine` |
| Trạng thái quy trình | `Order.Status` | `OrderState` + `Order.Status` |
| Compensation | Thủ công trong consumer | Tập trung trong saga |
| Debug / retry | Khó | Dễ (1 nơi) |
| Resume sau crash | Không | Có (saga instance) |
| Message / bước | 1 event | 1 command + 1 response |
