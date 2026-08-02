# TÀI LIỆU ĐẶC TẢ YÊU CẦU NGƯỜI DÙNG (URD) — ORCHESTRATION
## HỆ THỐNG XỬ LÝ & GIAO HÀNG ĐƠN HÀNG (ORDER FULFILLMENT) — SAGA PATTERN ORCHESTRATION + STATE MACHINE

---

## 1. TỔNG QUAN DỰ ÁN

### 1.1. Mục tiêu
Xây dựng hệ thống xử lý và giao hàng đơn hàng theo mô hình **Saga Pattern (Orchestration)** với **MassTransit SagaStateMachine** làm orchestrator trung tâm. Khác với hệ thống Choreography (3 bước, luồng tuyến tính), hệ thống này gồm **6 bước / 6 service, có nhánh điều kiện, cơ chế bồi hoàn (compensating actions) phức tạp theo thứ tự LIFO, và timeout tổng thể** — do đó cần một orchestrator tập trung quyết định thứ tự thực thi, gửi command, nhận response và xử lý bồi hoàn.

### 1.2. Lý do chọn Orchestration thay vì Choreography

| Tiêu chí | Phù hợp Choreography | Phù hợp Orchestration |
|:---------|:---------------------|:-----------------------|
| Số bước / service | Ít (2-3 bước) | Nhiều (5+ bước) |
| Nhánh điều kiện | Không / ít | Có (vd: kiểm tra gian lận) |
| Compensation | Đơn giản | Phức tạp, phải đúng thứ tự LIFO |
| Theo dõi tiến trình | Khó (rải rác log) | Dễ (1 nơi duy nhất) |
| Thêm bước mới | Phải sửa nhiều service | Chỉ sửa Saga |
| Single point of failure | Không có | Có (Orchestrator) — bù bằng saga instance lưu DB |

### 1.3. Công nghệ sử dụng
- **.NET** (Clean Architecture)
- **MassTransit + RabbitMQ** (Message Broker)
- **MassTransit SagaStateMachine** (Orchestrator + State Machine)
- **Entity Framework Core** (ORM, lưu saga instance + nghiệp vụ)
- **SignalR** (Real-time Notification)

---

## 2. DANH SÁCH SERVICE

| Service | Nhiệm vụ | Side-effect | Compensation |
|:--------|:---------|:------------|:-------------|
| **OrderService** | Tạo đơn, lưu Order/OrderItem, cập nhật trạng thái cuối | Ghi Order | Không cần |
| **FraudCheckService** | Kiểm tra gian lận giao dịch (nhánh pass/fail) | Ghi kết quả fraud | Không cần |
| **InventoryService** | Đặt trước (reserve) tồn kho | Ghi InventoryReservation | `ReleaseInventoryCommand` |
| **PaymentService** | Authorize trừ tiền | Ghi Payment | `RefundPaymentCommand` |
| **ShippingService** | Tạo đơn vận chuyển (carrier, tracking) | Ghi Shipment | `CancelShipmentCommand` |
| **NotificationService** | Gửi email/SMS/SignalR (best-effort) | Ghi Notification | Không cần |
| **OrderOrchestratorService** | Chứa `OrderFulfillmentSaga` (SagaStateMachine) | Lưu saga instance | — |

---

## 3. YÊU CẦU CHỨC NĂNG (FUNCTIONAL REQUIREMENTS)

### 3.1. Orchestrator khởi tạo saga
| ID | Yêu cầu | Mô tả |
|:---|:--------|:------|
| FR-01 | Nhận OrderCreatedEvent | UI gửi đơn qua OrderService -> publish `OrderCreatedEvent` -> Saga bắt đầu ở trạng thái `Submitted` |
| FR-02 | Gửi ValidateOrderCommand | Saga gửi command xác nhận đơn tới OrderService |

### 3.2. Kiểm tra gian lận
| ID | Yêu cầu | Mô tả |
|:---|:--------|:------|
| FR-03 | Gửi CheckFraudCommand | Saga gửi command tới FraudCheckService |
| FR-04 | Nhận kết quả | `FraudClearedEvent` -> đi tiếp; `FraudDetectedEvent` -> chuyển `Compensating` |

### 3.3. Đặt trước tồn kho (Reserve Inventory)
| ID | Yêu cầu | Mô tả |
|:---|:--------|:------|
| FR-05 | ReserveInventoryCommand | Saga yêu cầu InventoryService đặt trước tồn kho |
| FR-06 | Nhận kết quả | `InventoryReservedEvent` -> đi tiếp; `InventoryReservationFailedEvent` -> chuyển `Compensating` |

### 3.4. Thanh toán (Authorize Payment)
| ID | Yêu cầu | Mô tả |
|:---|:--------|:------|
| FR-07 | AuthorizePaymentCommand | Saga yêu cầu PaymentService trừ tiền |
| FR-08 | Nhận kết quả | `PaymentAuthorizedEvent` -> đi tiếp; `PaymentFailedEvent` -> bồi hoàn `ReleaseInventory` -> chuyển `Compensating` |

### 3.5. Tạo đơn vận chuyển (Create Shipment)
| ID | Yêu cầu | Mô tả |
|:---|:--------|:------|
| FR-09 | CreateShipmentCommand | Saga yêu cầu ShippingService tạo shipment |
| FR-10 | Nhận kết quả | `ShipmentCreatedEvent` -> CompleteOrder; `ShipmentFailedEvent` -> bồi hoàn `RefundPayment` + `ReleaseInventory` -> chuyển `Compensating` |

### 3.6. Hoàn tất & thông báo
| ID | Yêu cầu | Mô tả |
|:---|:--------|:------|
| FR-11 | CompleteOrderCommand | Saga yêu cầu OrderService đổi trạng thái `Completed` |
| FR-12 | Finalize | Nhận `OrderCompletedEvent` -> publish `SendNotificationCommand` -> kết thúc saga thành công |

### 3.7. Compensation (Bồi hoàn)
| ID | Yêu cầu | Mô tả |
|:---|:--------|:------|
| FR-13 | ReleaseInventory | Hoàn lại tồn kho đã reserve (khi Payment fail / Shipment fail / Timeout) |
| FR-14 | RefundPayment | Hoàn tiền (khi Shipment fail / Timeout) |
| FR-15 | CancelShipment | Hủy đơn vận chuyển (khi Timeout) |
| FR-16 | Thứ tự LIFO | Bồi hoàn theo thứ tự ngược lại các bước đã thành công |

### 3.8. Auto Timeout tổng thể
| ID | Yêu cầu | Mô tả |
|:---|:--------|:------|
| FR-17 | Saga timeout | Trong state bất kỳ (`DuringAny`), nếu quá N phút -> trigger `SagaTimeoutExpired` -> thực hiện toàn bộ bồi hoàn -> `Cancelled` |
| FR-18 | Saga instance | Trạng thái saga lưu vào DB (`OrderState`) để có thể resume sau khi service crash |

### 3.9. Real-time Notification
| ID | Yêu cầu | Mô tả |
|:---|:--------|:------|
| FR-19 | Notification | Tất cả sự kiện response đều kèm `NotificationPayLoad` -> NotificationService đẩy SignalR về UI |

### 3.10. Audit Trail
| ID | Yêu cầu | Mô tả |
|:---|:--------|:------|
| FR-20 | OrderHistory | Mỗi service/state transition ghi lại `OrderHistory` phục vụ debug và tracing |

---

## 4. SAGA FLOW (LUỒNG ORCHESTRATOR)

```
[UI] --(1) Tạo đơn--> [OrderService] --OrderCreatedEvent--> [OrderFulfillmentSaga]
                                                                │
   ┌────────────────────┬────────────────────┬───────────────────┤
   ▼                    ▼                    ▼                   ▼
1. ValidateOrder   2. CheckFraud       3. ReserveInventory  4. AuthorizePayment
   Command            Command              Command             Command
   -> OrderService    -> FraudCheck        -> Inventory        -> Payment
   <- Validated       <- Cleared/Detected  <- Reserved/Failed  <- Authorized/Failed
   ▼                    ▼                    ▼                   ▼
5. CreateShipment   6. CompleteOrder    7. Finalize -> SendNotification (email/SMS/SignalR)
   Command            Command
   -> Shipping        -> OrderService
   <- Created/Failed  <- Completed
                                                                │
                    (Bất kỳ bước nào fail / Timeout)           ▼
                                                                │
                                             ┌──────────────────▼──────────────┐
                                             │          COMPENSATING            │
                                             │  CancelShipment -> RefundPayment │
                                             │  -> ReleaseInventory             │
                                             └──────────────────┬──────────────┘
                                                                ▼
                                                          [CANCELLED / FAILED]
```

---

## 5. STATE MACHINE (SAGA STATE MACHINE)

```
                 ┌──────────────┐
    OrderCreated │  Submitted   │
    ────────────▶│              │
                 └──────┬───────┘
        ValidateOrder   ▼
      ┌─────────────────────────────┐
      │       ValidatingOrder       │
      └──────┬───────────────┬──────┘
 Validated   │               │  ValidationFailed
            ▼                ▼
 ┌─────────────────┐   ┌─────────────┐
 │  CheckingFraud  │   │ Compensating│──▶ CANCELLED
 └──────┬──────────┘   └─────────────┘
 FraudCleared ▼
 ┌──────────────────────┐
 │   ReservingInventory │
 └──────┬───────────────┘
 InventoryReserved ▼
 ┌──────────────────────┐
 │  AuthorizingPayment  │
 └──────┬───────────────┘
 PaymentAuthorized ▼
 ┌──────────────────────┐
 │   CreatingShipment   │
 └──────┬───────────────┘
 ShipmentCreated ▼
 ┌──────────────────────┐
 │       Completed      │──────────▶ (Final - thành công)
 └──────────────────────┘
        (DuringAny: SagaTimeoutExpired -> COMPENSATING -> CANCELLED)
```

### 5.1. Bảng transition

| State | Event đến | Hành động (saga gửi) | Transition tới |
|:------|:----------|:---------------------|:---------------|
| Submitted | OrderCreatedEvent | ValidateOrderCommand | ValidatingOrder |
| ValidatingOrder | OrderValidatedEvent | CheckFraudCommand | CheckingFraud |
| | OrderValidationFailedEvent | — | Compensating |
| CheckingFraud | FraudClearedEvent | ReserveInventoryCommand | ReservingInventory |
| | FraudDetectedEvent | — | Compensating |
| ReservingInventory | InventoryReservedEvent | AuthorizePaymentCommand | AuthorizingPayment |
| | InventoryReservationFailedEvent | — | Compensating |
| AuthorizingPayment | PaymentAuthorizedEvent | CreateShipmentCommand | CreatingShipment |
| | PaymentFailedEvent | ReleaseInventoryCommand | Compensating |
| CreatingShipment | ShipmentCreatedEvent | CompleteOrderCommand | (chờ CompletedEvent) |
| | ShipmentFailedEvent | RefundPaymentCommand + ReleaseInventoryCommand | Compensating |
| (chờ) | OrderCompletedEvent | SendNotificationCommand | Completed |
| Any (DuringAny) | SagaTimeoutExpired | CancelShipment + RefundPayment + ReleaseInventory | Cancelled |
| Compensating | (hoàn tất bồi hoàn LIFO) | — | Cancelled |

---

## 6. EVENT / COMMAND CATALOG

### 6.1. Commands (Saga → Service)
| Command | Dữ liệu | Mục đích |
|:--------|:--------|:---------|
| **ValidateOrderCommand** | OrderId, CustomerId, Items, TotalAmount | Kiểm tra tính hợp lệ của đơn |
| **CheckFraudCommand** | OrderId, CustomerId, TotalAmount | Kiểm tra gian lận |
| **ReserveInventoryCommand** | OrderId, Items | Đặt trước tồn kho |
| **AuthorizePaymentCommand** | OrderId, CustomerId, Amount | Trừ tiền |
| **CreateShipmentCommand** | OrderId, ShippingAddress, Items | Tạo đơn vận chuyển |
| **CompleteOrderCommand** | OrderId | Hoàn tất đơn |

### 6.2. Response Events (Service → Saga)
| Event | Dữ liệu | Mục đích |
|:------|:--------|:---------|
| **OrderCreatedEvent** | OrderId, CustomerId, Items, TotalAmount | Khởi tạo saga |
| **OrderValidatedEvent** / **OrderValidationFailedEvent** | OrderId, ... | Kết quả validate |
| **FraudClearedEvent** / **FraudDetectedEvent** | OrderId, Reason | Kết quả fraud |
| **InventoryReservedEvent** / **InventoryReservationFailedEvent** | OrderId, ReservationId, Reason | Kết quả reserve kho |
| **PaymentAuthorizedEvent** / **PaymentFailedEvent** | OrderId, PaymentId, Reason | Kết quả thanh toán |
| **ShipmentCreatedEvent** / **ShipmentFailedEvent** | OrderId, ShipmentId, TrackingNumber, Reason | Kết quả vận chuyển |
| **OrderCompletedEvent** | OrderId, Status | Hoàn tất thành công |

### 6.3. Compensation Commands
| Command | Mục đích |
|:--------|:---------|
| **ReleaseInventoryCommand** | Hoàn lại tồn kho đã reserve |
| **RefundPaymentCommand** | Hoàn tiền giao dịch |
| **CancelShipmentCommand** | Hủy đơn vận chuyển |

### 6.4. Notification
| Command | Mục đích |
|:--------|:---------|
| **SendNotificationCommand** | Gửi email/SMS/SignalR kèm `NotificationPayLoad` (best-effort, không compensate) |

### 6.5. Notification Payload
```csharp
NotificationPayLoad(TargetUserId, Title, Message, NotificationType, Timestamp)
```

---

## 7. COMPENSATION MATRIX (MA TRẬN BỒI HOÀN — LIFO)

| Bước đã thành công | Khi bước kế fail | Bồi hoàn cần thực hiện |
|:--------------------|:------------------|:-----------------------|
| ValidateOrder | Validation fail | Không cần (chưa có side-effect) |
| FraudCheck | Fraud detected | Không cần |
| **ReserveInventory** | Payment fail / Shipment fail / Timeout | `ReleaseInventoryCommand` |
| **AuthorizePayment** | Shipment fail / Timeout | `RefundPaymentCommand` |
| **CreateShipment** | Complete fail / Timeout | `CancelShipmentCommand` |

> Thứ tự bồi hoàn luôn **LIFO** (Last-In-First-Out): bước hoàn thành sau cùng sẽ được bồi hoàn trước.

---

## 8. CẤU TRÚC DỮ LIỆU (DATA MODEL)

### 8.1. OrderState (Saga Instance — MassTransit)
| Trường | Kiểu | Mô tả |
|:-------|:-----|:------|
| CorrelationId | Guid | Khóa chính (= OrderId) |
| CurrentState | string | State hiện tại của saga |
| ReservationId | Guid? | Tham chiếu InventoryReservation |
| PaymentId | Guid? | Tham chiếu Payment |
| ShipmentId | Guid? | Tham chiếu Shipment |
| CreatedAt | DateTime | Thời điểm saga bắt đầu |

### 8.2. Order (Kế thừa hệ thống Choreography)
| Trường | Kiểu | Mô tả |
|:-------|:-----|:------|
| OrderId | Guid | Khóa chính |
| CustomerId | string | ID khách hàng |
| Status | OrderStatus | Submitted, Accepted, Completed, Rejected, Cancelled |
| TotalAmount | decimal | Tổng tiền |
| ShippingAddress | string | Địa chỉ giao hàng |
| OrderItems | ICollection\<OrderItem> | Danh sách sản phẩm |

### 8.3. InventoryReservation (Đặt trước tồn kho)
| Trường | Kiểu | Mô tả |
|:-------|:-----|:------|
| ReservationId | Guid | Khóa chính |
| OrderId | Guid | FK -> Order |
| ProductId | Guid | Sản phẩm |
| Quantity | int | Số lượng đặt trước |
| Status | ReservationStatus | Reserved / Released |

### 8.4. Payment (Thanh toán)
| Trường | Kiểu | Mô tả |
|:-------|:-----|:------|
| PaymentId | Guid | Khóa chính |
| OrderId | Guid | FK -> Order |
| Amount | decimal | Số tiền |
| TransactionId | string | Mã giao dịch cổng thanh toán |
| Status | PaymentStatus | Authorized / Refunded / Failed |

### 8.5. Shipment (Vận chuyển)
| Trường | Kiểu | Mô tả |
|:-------|:-----|:------|
| ShipmentId | Guid | Khóa chính |
| OrderId | Guid | FK -> Order |
| CarrierCode | string | Hãng vận chuyển |
| TrackingNumber | string | Mã vận đơn |
| Status | ShipmentStatus | Created / Cancelled |

### 8.6. Notification & OrderHistory (Kế thừa hệ thống Choreography)
- `Notification` (NotifyId, OrderId, TargetUserId, Title, Message, Type, IsRead, SendAt)
- `OrderHistory` (HistoryId, OrderId, ConsumerName, Status, EventType, Message)

---

## 9. DANH SÁCH ENUMS

| Enum | Giá trị |
|:-----|:--------|
| **OrderStatus** | Submitted = 1, Accepted = 2, Completed = 3, Rejected = 4, Cancelled = 5 |
| **ReservationStatus** | Reserved = 1, Released = 2 |
| **PaymentStatus** | Authorized = 1, Refunded = 2, Failed = 3 |
| **ShipmentStatus** | Created = 1, Cancelled = 2 |
| **HistoryStatus** | Success = 1, Failed = 2 |
| **SagaState** | Submitted, ValidatingOrder, CheckingFraud, ReservingInventory, AuthorizingPayment, CreatingShipment, Compensating, Completed, Cancelled |

---

## 10. YÊU CẦU PHI CHỨC NĂNG (NON-FUNCTIONAL REQUIREMENTS)

| ID | Yêu cầu | Mô tả |
|:---|:--------|:------|
| NFR-01 | Eventually Consistent | Saga đảm bảo dữ liệu cuối cùng nhất quán qua compensating actions |
| NFR-02 | Độ bền của saga | Saga instance lưu DB -> resume sau crash; MassTransit hỗ trợ retry/delay |
| NFR-03 | Xử lý lỗi & Retry | Mỗi service bắt lỗi, trả Response tương ứng; orchestrator quyết định compensate |
| NFR-04 | Real-time Updates | SignalR đẩy thông báo ngay khi trạng thái thay đổi |
| NFR-05 | Audit Trail | Mọi state transition ghi vào OrderHistory |

---

## 11. SƠ ĐỒ KIẾN TRÚC

```
[UI / Client] ────────────── Tạo đơn / nhận SignalR ───────────────────┐
     │                                                               │
     ▼                                                               ▼
[OrderService] ──OrderCreatedEvent──▶ ┌───────────────────────────┐ [NotificationService]
      ▲                               │  OrderOrchestratorService │   (SignalR Hub)
      │ Command                        │  [OrderFulfillmentSaga]  │
      └──────────◀────────────────────┤  (SagaStateMachine)       │
                                      └──────┬───────┬───────┬────┘
                                             │       │       │ Command
        ┌────────────────────────────────────┘       │       └──────────────┐
        ▼                                            ▼                      ▼
[FraudCheckService]                        [InventoryService]        [ShippingService]
                                            [PaymentService]
                                            [OrderService]
        └──────────────────────────────────▶ RabbitMQ ──────────────▶ Response Events → Saga
```

---

## 12. RÀNG BUỘC & GIẢ ĐỊNH

1. Mỗi đơn hàng phải có ít nhất 1 sản phẩm (OrderItem)
2. Tồn kho phải đủ để đặt trước (reserve) trước khi thanh toán
3. Thanh toán chỉ thực hiện sau khi reserve kho thành công
4. Vận chuyển chỉ tạo sau khi thanh toán thành công
5. Mọi bồi hoàn thực hiện theo thứ tự LIFO
6. Timeout tổng thể N phút áp dụng cho toàn bộ saga (`DuringAny`)
7. Thông báo (Notification) là best-effort, không có compensation
8. Saga instance (`OrderState`) là nguồn sự thật duy nhất về tiến trình saga

---

## 13. ĐỐI CHIẾU VỚI HỆ THỐNG CHOREOGRAPHY (URD.md)

| Đặc điểm | Choreography (URD.md) | Orchestration (tài liệu này) |
|:---------|:----------------------|:-----------------------------|
| Số bước | 3 | 6 |
| Nhánh điều kiện | Không | Có (FraudCheck) |
| Ai quyết định bước tiếp | Từng consumer tự publish | SagaStateMachine tập trung |
| Compensation | Đơn giản, thủ công | LIFO, tập trung trong saga |
| State lưu ở đâu | `Order.Status` trong DB | Saga instance `OrderState` |
| Theo dõi & debug | Khó | Dễ (1 nơi) |
| Phù hợp khi | Luồng ngắn, service độc lập | Luồng dài, nhiều nhánh, cần quản lý lỗi tập trung |
