# Hệ Thống Saga Order - 4 Flow Chính (Mermaid)

---

## ✅ 1. HAPPY PATH - Thành Công Hoàn Toàn

```mermaid
sequenceDiagram
    autonumber
    actor Client
    participant Orchestrator as OrderOrchestration<br/>(Saga State Machine)
    participant SubmitSvc as OrderSubmitService<br/>(Validate)
    participant AcceptSvc as OrderAcceptService<br/>(Accept + Timer)
    participant CompleteSvc as OrderCompleteService<br/>(Complete + Trừ kho)
    participant NotifSvc as NotificationService<br/>(SignalR)

    Client->>Orchestrator: POST /api/orders → OrderCreatedEvent
    Orchestrator->>Orchestrator: Initially → OrderCreatedActivity
    Orchestrator->>SubmitSvc: Send ValidateOrderCommand<br/>(queue:order-validation-queue)
    SubmitSvc->>SubmitSvc: Check sản phẩm tồn tại + IsActive<br/>Hàng ĐÃ reserve khi tạo đơn (IsReserved=true)
    SubmitSvc-->>Orchestrator: Publish OrderValidatedEvent
    Orchestrator->>Orchestrator: During(Validating) → OrderValidated<br/>StepsCompleted=1
    Orchestrator->>AcceptSvc: Send AcceptOrderCommand<br/>(queue:order-accept-queue)
    Orchestrator-->>NotifSvc: Publish OrderSubmitSuccessResponse (SignalR)
    AcceptSvc->>AcceptSvc: Re-validate sản phẩm
    AcceptSvc->>AcceptSvc: Order.Status = Accepted
    AcceptSvc->>AcceptSvc: Tạo OrderTimer (Timeout = config)
    AcceptSvc-->>Orchestrator: Publish OrderAcceptedEvent (có TimeoutMinutes)
    Orchestrator->>Orchestrator: During(Accepting) → OrderAccepted<br/>StepsCompleted=2
    Orchestrator->>CompleteSvc: Send CompleteOrderCommand<br/>(queue:order-complete-queue)
    Orchestrator-->>NotifSvc: Publish OrderAcceptSuccessResponse (SignalR)
    CompleteSvc->>CompleteSvc: Re-validate, trừ PhysicalQty & ReservedQty
    CompleteSvc->>CompleteSvc: Order.Status = Completed, IsReserved=false
    CompleteSvc->>CompleteSvc: Cancel OrderTimer
    CompleteSvc-->>Orchestrator: Publish OrderCompletedEvent
    Orchestrator->>Orchestrator: During(Completing) → OrderCompleted<br/>StepsCompleted=3
    Orchestrator-->>NotifSvc: Publish OrderCompleteSuccessResponse (SignalR)
    NotifSvc-->>Client: Push SignalR real-time: "Đơn hàng hoàn tất"
```

---

## ❌ 2. FAIL PATH - Thất Bại Nghiệp Vụ

### 2.1 Fail tại Validate (Step 0 - Không compensation)

```mermaid
sequenceDiagram
    autonumber
    participant Orchestrator as OrderOrchestration
    participant SubmitSvc as OrderSubmitService

    Orchestrator->>SubmitSvc: ValidateOrderCommand
    SubmitSvc->>SubmitSvc: Sản phẩm không tồn tại / IsActive=false
    SubmitSvc->>SubmitSvc: Release ReservedQty (mở khóa hàng)
    SubmitSvc->>SubmitSvc: Order.Status = Rejected, IsReserved=false
    SubmitSvc-->>Orchestrator: Publish OrderValidationFailedEvent
    Orchestrator->>Orchestrator: During(Validating) → OrderValidationFailed<br/>StepsCompleted=0 → KHÔNG compensation
    Orchestrator->>Orchestrator: TransitionTo(Rejected) ✅ FINAL
```

### 2.2 Fail tại Accept (Step 1 - Có compensation LIFO)

```mermaid
sequenceDiagram
    autonumber
    participant Orchestrator as OrderOrchestration
    participant AcceptSvc as OrderAcceptService
    participant ReleaseSvc as OrderCompleteService<br/>(ReleaseInventoryConsumer)
    participant CancelSvc as OrderAcceptService<br/>(CancelOrderConsumer)

    Orchestrator->>AcceptSvc: AcceptOrderCommand
    AcceptSvc->>AcceptSvc: Sản phẩm bị xóa/vô hiệu hóa
    AcceptSvc->>AcceptSvc: Order.Status = Rejected
    AcceptSvc-->>Orchestrator: Publish OrderAcceptFailedEvent
    
    Orchestrator->>Orchestrator: During(Accepting) → OrderAcceptFailed<br/>StepsCompleted=1 ≥ 1 → TRIGGER COMPENSATION
    
    rect rgb(255, 240, 240)
        note right of Orchestrator: COMPENSATION LIFO BẮT ĐẦU
        
        Orchestrator->>ReleaseSvc: ReleaseInventoryCompensateActivity<br/>Send ReleaseInventoryCommand
        Orchestrator->>Orchestrator: TransitionTo(CompensatingRelease)
        
        ReleaseSvc->>ReleaseSvc: Chỉ chạy nếu IsReserved=true (idempotent)
        ReleaseSvc->>ReleaseSvc: Giảm ReservedQty, Order.Status = Accepted
        ReleaseSvc-->>Orchestrator: Publish InventoryReleasedEvent
        
        Orchestrator->>Orchestrator: During(CompensatingRelease) → InventoryReleased
        Orchestrator->>CancelSvc: CancelOrderCompensateActivity<br/>Send CancelOrderCommand
        Orchestrator->>Orchestrator: TransitionTo(CompensatingCancel)
        
        CancelSvc->>CancelSvc: Chỉ chạy nếu Status=Accepted (idempotent)
        CancelSvc->>CancelSvc: Order.Status = Rejected, Cancel Timer
        CancelSvc-->>Orchestrator: Publish OrderCancelledEvent
        
        Orchestrator->>Orchestrator: During(CompensatingCancel) → OrderCancelled
        Orchestrator->>Orchestrator: TransitionTo(Rejected) ✅ FINAL
    end
```

### 2.3 Fail tại Complete (Step 2 - Compensation LIFO đầy đủ)

```mermaid
sequenceDiagram
    autonumber
    participant Orchestrator as OrderOrchestration
    participant CompleteSvc as OrderCompleteService
    participant ReleaseSvc as OrderCompleteService<br/>(ReleaseInventoryConsumer)
    participant CancelSvc as OrderAcceptService<br/>(CancelOrderConsumer)

    Orchestrator->>CompleteSvc: CompleteOrderCommand
    CompleteSvc->>CompleteSvc: Lỗi validate / trừ kho
    CompleteSvc-->>Orchestrator: Publish OrderCompleteFailedEvent
    
    Orchestrator->>Orchestrator: During(Completing) → OrderCompleteFailed<br/>StepsCompleted=2 ≥ 1 → COMPENSATION LIFO
    
    rect rgb(255, 240, 240)
        note right of Orchestrator: COMPENSATION LIFO: Complete → Accept → Validate
        
        Orchestrator->>ReleaseSvc: ReleaseInventoryCompensateActivity
        Orchestrator->>Orchestrator: TransitionTo(CompensatingRelease)
        ReleaseSvc-->>Orchestrator: InventoryReleasedEvent
        
        Orchestrator->>CancelSvc: CancelOrderCompensateActivity
        Orchestrator->>Orchestrator: TransitionTo(CompensatingCancel)
        CancelSvc-->>Orchestrator: OrderCancelledEvent
        
        Orchestrator->>Orchestrator: TransitionTo(Rejected) ✅ FINAL
    end
```

---

## ⏱️ 3. TIMEOUT PATH - Hết Thời Gian Chờ

```mermaid
sequenceDiagram
    autonumber
    participant Orchestrator as OrderOrchestration<br/>(DuringAny - Bắt TẤT CẢ state)
    participant TimerSvc as Background Timer Service
    participant ReleaseSvc as OrderCompleteService<br/>(ReleaseInventoryConsumer)
    participant CancelSvc as OrderAcceptService<br/>(CancelOrderConsumer)

    Note over AcceptSvc: Tạo OrderTimer với Timeout = Now + Config
    
    TimerSvc->>Orchestrator: Publish OrderTimeoutExpiredEvent<br/>(OrderId, CustomerId)
    
    alt StepsCompleted = 0 (Timeout ở Validating)
        Orchestrator->>Orchestrator: DuringAny → OrderTimeoutExpired
        Orchestrator->>Orchestrator: IfElse(StepsCompleted >= 1) → FALSE
        Orchestrator->>Orchestrator: TransitionTo(Rejected) ✅ FINAL<br/>KHÔNG compensation
    else StepsCompleted ≥ 1 (Timeout ở Accepting/Completing)
        Orchestrator->>Orchestrator: DuringAny → OrderTimeoutExpired
        Orchestrator->>Orchestrator: IfElse(StepsCompleted >= 1) → TRUE
        Orchestrator->>Orchestrator: OrderTimeoutExpiredActivity: Publish Timeout Response
        Orchestrator->>ReleaseSvc: ReleaseInventoryCompensateActivity
        Orchestrator->>Orchestrator: TransitionTo(CompensatingRelease)
        
        rect rgb(255, 240, 240)
            note right of Orchestrator: COMPENSATION TƯƠNG TỰ FAIL PATH
            ReleaseSvc-->>Orchestrator: InventoryReleasedEvent
            Orchestrator->>CancelSvc: CancelOrderCompensateActivity
            Orchestrator->>Orchestrator: TransitionTo(CompensatingCancel)
            CancelSvc-->>Orchestrator: OrderCancelledEvent
            Orchestrator->>Orchestrator: TransitionTo(Rejected) ✅ FINAL
        end
    end
```

---

## 🔄 4. COMPENSATION PATH - Bồi Hoàn LIFO (Chi Tiết)

```mermaid
flowchart TD
    subgraph FAIL_TRIGGER["❌ Trigger Compensation (Fail/Timeout)"]
        A1[OrderAcceptFailedEvent] 
        A2[OrderCompleteFailedEvent]
        A3[OrderTimeoutExpiredEvent<br/>(StepsCompleted ≥ 1)]
    end

    subgraph ORCHESTRATOR["OrderOrchestration State Machine"]
        B1[During(Accepting/Completing/Any)<br/>IfElse StepsCompleted ≥ 1]
        B2[ReleaseInventoryCompensateActivity<br/>Send ReleaseInventoryCommand]
        B3[TransitionTo CompensatingRelease]
        B4[During CompensatingRelease<br/>Wait InventoryReleasedEvent]
        B5[CancelOrderCompensateActivity<br/>Send CancelOrderCommand]
        B6[TransitionTo CompensatingCancel]
        B7[During CompensatingCancel<br/>Wait OrderCancelledEvent]
        B8[TransitionTo Rejected FINAL]
    end

    subgraph RELEASE["ReleaseInventoryConsumer<br/>(OrderCompleteService)"]
        C1[Nhận ReleaseInventoryCommand]
        C2{IsReserved == true?}
        C3[Giảm ReservedQty cho từng item]
        C4[Order.IsReserved = false<br/>Order.Status = Accepted]
        C5[SaveChanges DB]
        C6[Publish InventoryReleasedEvent]
        C7[Bỏ qua (idempotent)<br/>Publish InventoryReleasedEvent]
    end

    subgraph CANCEL["CancelOrderConsumer<br/>(OrderAcceptService)"]
        D1[Nhận CancelOrderCommand]
        D2{Status == Accepted?}
        D3[Order.Status = Rejected<br/>RejectedAt = Now]
        D4[Timer.TimerStatus = Cancelled]
        D5[SaveChanges DB]
        D6[Publish OrderCancelledEvent]
        D7[Bỏ qua (idempotent)]
    end

    subgraph ERROR_HANDLING["Xử Lý Lỗi Compensation"]
        E1[InventoryReleasedFailedEvent<br/>→ Publish OrderAcceptFailedResponse<br/>→ TransitionTo Rejected]
        E2[CancelOrderFailedEvent<br/>→ Publish OrderAcceptFailedResponse<br/>→ TransitionTo Rejected]
    end

    FAIL_TRIGGER --> B1
    B1 --> B2
    B2 --> B3
    B3 --> C1
    C1 --> C2
    C2 -- Yes --> C3
    C3 --> C4
    C4 --> C5
    C5 --> C6
    C2 -- No --> C7
    C6 --> B4
    C7 --> B4
    B4 --> B5
    B5 --> B6
    B6 --> D1
    D1 --> D2
    D2 -- Yes --> D3
    D3 --> D4
    D4 --> D5
    D5 --> D6
    D2 -- No --> D7
    D6 --> B7
    D7 --> B7
    B7 --> B8
    B8 --> END([✅ Rejected Final])
    
    %% Error paths
    C1 -.->|Exception| E1
    D1 -.->|Exception| E2
    E1 --> B8
    E2 --> B8

    style FAIL_TRIGGER fill:#ffebee
    style ORCHESTRATOR fill:#e3f2fd
    style RELEASE fill:#fff3e0
    style CANCEL fill:#fff3e0
    style ERROR_HANDLING fill:#fce4ec
```

---

## 📋 Tóm Tắt State Transition Table

```mermaid
stateDiagram-v2
    [*] --> Initially: OrderCreatedEvent
    
    Initially --> Validating: OrderCreatedActivity
    
    Validating --> Accepting: OrderValidatedEvent (Step=1)
    Validating --> Rejected: OrderValidationFailedEvent (Step=0, No Compensate)
    Validating --> Rejected: OrderTimeoutExpiredEvent (Step=0, No Compensate)
    
    Accepting --> Completing: OrderAcceptedEvent (Step=2)
    Accepting --> CompensatingRelease: OrderAcceptFailedEvent (Step=1, Compensate)
    Accepting --> CompensatingRelease: OrderTimeoutExpiredEvent (Step=1, Compensate)
    
    Completing --> Completed: OrderCompletedEvent (Step=3)
    Completing --> CompensatingRelease: OrderCompleteFailedEvent (Step=2, Compensate)
    Completing --> CompensatingRelease: OrderTimeoutExpiredEvent (Step=2, Compensate)
    
    CompensatingRelease --> CompensatingCancel: InventoryReleasedEvent
    CompensatingRelease --> Rejected: InventoryReleasedFailedEvent
    
    CompensatingCancel --> Rejected: OrderCancelledEvent
    CompensatingCancel --> Rejected: CancelOrderFailedEvent
    
    Completed --> [*]: Final Success
    Rejected --> [*]: Final Failure/Compensated
```

---

## 🔑 Key Points

| Flow | Trigger | Compensation | Final State |
|------|---------|--------------|-------------|
| **Happy** | Tất cả success | Không | `Completed` |
| **Fail Validate** | ValidationFailed | Không (Step=0) | `Rejected` |
| **Fail Accept** | AcceptFailed | ReleaseInv → Cancel | `Rejected` |
| **Fail Complete** | CompleteFailed | ReleaseInv → Cancel | `Rejected` |
| **Timeout (Step=0)** | TimeoutExpired | Không | `Rejected` |
| **Timeout (Step≥1)** | TimeoutExpired | ReleaseInv → Cancel | `Rejected` |

**LIFO Order:** Complete → Accept → Validate (ngược lại thứ tự thực hiện)
**Idempotency:** Mọi consumer đều check trạng thái trước khi xử lý (tránh duplicate/retry)
**Error in Compensation:** Nếu compensation fail → Saga dừng, publish error response, vẫn về `Rejected`