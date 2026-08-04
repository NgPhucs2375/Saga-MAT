# Saga Flow — Order Orchestration

## 1) Happy path

```mermaid
sequenceDiagram
    participant UI as UI/WebApp
    participant S as Saga (Orchestrator)
    participant Sub as OrderSubmitService
    participant Acc as OrderAcceptService
    participant Com as OrderCompleteService
    participant Noti as NotificationService

    UI->>S: publish OrderCreatedEvent
    S->>Sub: Send ValidateOrderCommand (order-validation-queue)
    Sub-->>S: publish OrderValidatedEvent
    S->>Acc: Send AcceptOrderCommand (order-accept-queue)
    Acc-->>S: publish OrderAcceptedEvent
    Note over S: set NeedReleaseInventory=true
    S->>Com: Send CompleteOrderCommand (order-complete-queue)
    Com-->>S: publish OrderCompletedEvent
    S->>Noti: publish OrderCompleteSuccessResponse
    Noti-->>UI: SignalR
```

## 2) Complete fail — bồi hoàn LIFO 2 bước

```mermaid
sequenceDiagram
    participant S as Saga (Orchestrator)
    participant Com as OrderCompleteService
    participant Acc as OrderAcceptService
    participant Noti as NotificationService

    Com-->>S: publish OrderCompleteFailedEvent
    Note over S: Completing -> Compensating, set ErrorReason, NeedReleaseInventory
    S->>Com: Send ReleaseInventoryCommand (order-release-inventory-queue)
    Note over Com: ReleaseInventoryConsumer: cộng lại SLTKho, Order Completed->Accepted
    Com-->>S: publish InventoryReleasedEvent
    S->>Acc: Send CancelOrderCommand (order-cancel-queue)
    Note over Acc: CancelOrderConsumer: Order Accepted->Rejected, hủy timer
    Acc-->>S: publish OrderCancelledEvent
    S->>Noti: publish OrderCompleteFailedResponse
    Note over S: -> Cancelled
```

## 3) Timeout — Saga quyết định

```mermaid
sequenceDiagram
    participant TW as TimerWatcherBackgroundService
    participant S as Saga (Orchestrator)
    participant Com as OrderCompleteService
    participant Acc as OrderAcceptService
    participant Noti as NotificationService

    TW-->>S: publish OrderTimeoutExpiredEvent(TargetAction)
    alt TargetAction = Complete
        S->>Com: Send CompleteOrderCommand -> Completing (tiếp happy path)
    else TargetAction = Reject
        S->>S: OrderTimeoutExpiredActivity, IsTimeout=true -> Compensating
        alt NeedReleaseInventory = true
            S->>Com: Send ReleaseInventoryCommand (LIFO 1)
            Com-->>S: publish InventoryReleasedEvent
        end
        S->>Acc: Send CancelOrderCommand (LIFO 2)
        Acc-->>S: publish OrderCancelledEvent
        S->>Noti: publish OrderAcceptFailedResponse (timeout)
        Note over S: -> Cancelled
    end
```

## 4) State machine

```mermaid
stateDiagram-v2
    [*] --> Validating: OrderCreatedEvent
    Validating --> Accepting: OrderValidatedEvent
    Validating --> Cancelled: OrderValidationFailedEvent (không cần bù)
    Accepting --> Completing: OrderAcceptedEvent (NeedReleaseInventory=true)
    Accepting --> Cancelled: OrderAcceptFailedEvent (không có side-effect)
    Completing --> Completed: OrderCompletedEvent
    Completing --> Compensating: OrderCompleteFailedEvent
    Compensating --> Compensating: InventoryReleasedEvent -> Send CancelOrderCommand
    Compensating --> Cancelled: OrderCancelledEvent -> publish response
    Compensating --> Cancelled: ReleaseInventoryFailedEvent / CancelOrderFailedEvent
    state "DuringAny" as DA {
        [*] --> T1: OrderTimeoutExpiredEvent (TargetAction=Complete) -> Send CompleteOrderCommand -> Completing
        [*] --> T2: OrderTimeoutExpiredEvent (Reject) -> Compensating
    }
    Completed --> [*]
    Cancelled --> [*]
```

## 5) Queue map

| Queue | Producer | Consumer |
|---|---|---|
| `order-validation-queue` | Saga (`OrderCreatedActivity`) | `OrderSubmitService` |
| `order-accept-queue` | Saga (`OrderValidatedActivity`) | `OrderAcceptService` |
| `order-complete-queue` | Saga (`OrderAcceptedActivity`, `OrderTimeoutCompleteActivity`) | `OrderCompleteService` |
| `order-release-inventory-queue` | Saga (`OrderCompleteFailedActivity`, `OrderTimeoutExpiredActivity`) | `OrderCompleteService` (`ReleaseInventoryConsumer`) |
| `order-cancel-queue` | Saga (`InventoryReleasedActivity`, `OrderTimeoutExpiredActivity`) | `OrderAcceptService` (`CancelOrderConsumer`) |
| `notification-queue` | Các luồng publish `*Response` | `NotificationService` |

## 6) Idempotency guard

| Consumer | Chỉ xử lý khi Order trạng thái |
|---|---|
| `OrderAcceptConsumer` | `Submitted` |
| `OrderCompleteConsumer` | `Accepted` |
| `ReleaseInventoryConsumer` | `Completed` |
| `CancelOrderConsumer` | `Accepted` |

---

# OVERVIEW: Nâng cấp MassTransit `8.5.7` → `9.2.0`

> Tóm tắt cho toàn repo: **không phải "nâng lên là vỡ ngay lập tức ở cấp biên dịch", nhưng có 3 nhóm vấn đề BẮT BUỘC xử lý:**
> ① **Giấy phép thương mại (Commercial License)** — v9 giờ là sản phẩm thương mại của cty Massient, yêu cầu license key (mã nguồn mở chính thức v8 kết thúc hỗ trợ sau 2026).
> ② **Thay đổi schema DB** (saga `OrderState`, bảng SQL transport, outbox) → phải chạy migration/hosted service tạo lại.
> ③ **Nâng cấp đồng bộ toàn bộ dịch vụ** cùng 1 phiên bản major, không được để server/choreography lẫn lộn v9/v8.

Các API dự án này đang dùng (`UsingRabbitMq`, `UsingPostgres`, `cfg.Host(...)`, `SqlTransportOptions`, `EntityFrameworkRepository(...)`, `IStateMachineActivity<TInstance,TMessage>`, `.Activity(x=>x.OfType<T>())`, `SagaClassMap`) **vẫn tồn tại trong tài liệu v9** → khả năng cao biên dịch được, nhưng vẫn cần `dotnet build` để soi lại.

---

# DETAILS: Từng thay đổi ảnh hưởng tới hệ thống hiện tại

## A. VỀ GIẤY PHÉP (BẬT ỨNG DỤNG)
| Vấn đề | 8.5.7 | 9.2.0 | Ảnh hưởng |
|---|---|---|---|
| License | Apache-2.0 mã nguồn mở, tự do | Thương mại (Massient), cần **license key**; `AddMassTransit` có validate license | Dev local có key eval miễn phí, nhưng **deploy production cần trả phí** (thấp nhất ~$400/tháng) |
| Hỗ trợ | còn bảo trì | v8 còn patch đến hết 2026 rồi EOL | Nếu ngân sách hạn chế, cân nhắc giữ v8 |

## B. TARGET FRAMEWORK
- v9 có target **`net10.0`** — khớp với các project của bạn (`TargetFramework=net10.0`). ✅ Không vấn đề.

## 3. API BỊ ĐỔI TÊN / GỠ (thấp khả năng trúng, đã kiểm tra dự án này KHÔNG dùng)
| Mục | Ghi chú | Trong repo? |
|---|---|---|
| `AddSendEndpointConvention` → `AddMessageRoute` (9.0.1) | không dùng | ❌ |
| `TransactionalBus` bị gỡ (9.2) | không dùng | ❌ |
| `IPublishEndpoint`/`ISendEndpointProvider` đổi routing ở MultiBus (9.2 breaking) | repo chỉ dùng **single bus** nên không ăn | ❌ |
| JSON serialize chuyển sang per-bus/endpoint (backward-compatible) | dùng dạng mặc định | ❌ |

## 4. SCHEMA DATABASE — **PHẢI XỬ LÝ**
- Changelog v9.1 ghi rõ: *"transactional outbox tables were updated; apply migrations before rollout"*.
- Bảng saga **`OrderState`** (EF): v9 có thể thêm column mới (VD `RowVersion`, `FaultMessage`, `RetryCount`…) → **phải regenerate EF migration** cho cả `OrderSagaDbContext`.
- SQL transport tự tạo schema/table/functions riêng (do `AutoStart` / hosted service tạo): nâng lên v9 nên dùng lại cơ chế tạo schema chuẩn mới.
- Khi có migration DB mới → cần làm `dotnet ef migrations add`/`database update` tương ứng cho cả **2 DbContext** (`ApplicationDbContext`, `IdentityContext`) và **saga DbContext** trước khi chạy.

## 5. CẤU HÌNH SQL TRANSPORT (các Consumer + WebApp)
- Hiện tại: `services.Configure<SqlTransportOptions>` + `cfg.AutoStart = true`.
- v9 khuyên dùng **`services.AddPostgresMigrationHostedService()` đặt TRƯỚC `AddMassTransit`** để tạo schema/tables trước khi bus start; `AutoStart` bị coi là deprecated.
- `SqlTransportOptions.ConnectionString` vẫn đúng chuẩn options pattern → giữ nguyên.

## 6. TƯƠNG THÍCH WIRE GIỮA CÁC SERVICE (QUAN TRỌNG)
- `OrderOrchestration` dùng transport **RabbitMQ** + saga EF; các Consumer (`OrderSubmit/Accept/Complete`, `NotificationService`) và `WebApp.Server` dùng transport **SQL Postgres** — message đi qua RabbitMQ và định dạng envelope khác nhau.
- Phải **nâng cả giải (toàn bộ project cùng lúc)**, không nâng lẻ. Nếu lẫn `lồng v8` (vd producer 9.2 gửi, consumer 8.5.7 nhận) → deserialize fail → message rơi vào `_error` queue → saga không hoàn tất.
- Kiểm tra lại bằng cách run cả stack từ A→Z (Happy path + Complete fail + Timeout) ngay sau khi build.

## 7. ĐỀ XUẤT CÁC BƯỚC LÀM AN TOÀN
1. Nhân bản/branch riêng, `dotnet restore` sau khi đổi version 9.2.0 ở 7 csproj (OrderOrchestration, OrderSubmit/Accept/Complete Service, NotificationService, Application, WebApp.Server).
2. `dotnet build` toàn solution → soi chỗ sai API (nếu có).
3. Cập license key (nếu production).
4. Tạo lại migrations saga/transport; `database update`.
5. Chạy toàn bộ stack, verify 4 kịch bản Happy path + 2 fail + timeout + check Saga `OrderState` gửi đúng.

## 8. BẢNG TỔNG HỢP TÁC ĐỘNG THEO PROJECT

| Project | Package (8.5.7) | Thay đổi API cơ bản? | Schema cần xử? |
|---|:---|:---:|:---:|
| `OrderOrchestration` | MassTransit, .EntityFrameworkCore, .SqlTransport.PostgreSQL | Saga repository / `UsingRabbitMq` OK | ✅ `OrderState` saga |
| `OrderSubmit/Accept/Complete Service` | MassTransit, .RabbitMQ, .SqlTransport.PostgreSQL | `UsingPostgres`, `AutoStart` (deprecated) | ✅ transport + outbox |
| `NotificationService` | MassTransit, .SqlTransport.PostgreSQL | `UsingPostgres`, `ReceiveEndpoint` OK | ✅ transport |
| `WebApp.Server` | MassTransit, .RabbitMQ, .EntityFrameworkCore, .SqlTransport.PostgreSQL | `SqlTransportOptions` OK | ✅ outbox |
| `Infrastructure.Persistence` | `OpenTelemetry.Instrumentation.MassTransit` (beta.3) | không phải package MassTransit, không cần nâng | - |
