# TÀI LIỆU ĐẶC TẢ YÊU CẦU NGƯỜI DÙNG (URD)
## HỆ THỐNG XỬ LÝ ĐƠN HÀNG SAGA PATTERN - EVENT DRIVEN ARCHITECTURE

---

## 1. TỔNG QUAN DỰ ÁN

### 1.1. Mục tiêu
Xây dựng hệ thống xử lý đơn hàng theo mô hình **Saga Pattern (Choreography)** sử dụng **Event-Driven Architecture** với **MassTransit/RabbitMQ** làm message broker. Hệ thống cho phép người dùng tạo đơn hàng, tự động xử lý qua chuỗi các Consumer, cập nhật trạng thái real-time qua **SignalR**.

### 1.2. Công nghệ sử dụng
- **.NET** (Clean Architecture)
- **MassTransit + RabbitMQ** (Message Broker)
- **Entity Framework Core** (ORM)
- **SignalR** (Real-time Notification)
- **CQRS Pattern**

---

## 2. YÊU CẦU CHỨC NĂNG (FUNCTIONAL REQUIREMENTS)

### 2.1. Tính năng: Tạo đơn hàng (Submit Order)

| ID | Yêu cầu | Mô tả |
|:---|:--------|:------|
| FR-01 | Người dùng gửi đơn hàng | UI/API phát ra `OrderSubmittedEvent` chứa danh sách sản phẩm (ProductId, Quantity, UnitPrice) |
| FR-02 | Kiểm tra sản phẩm tồn tại | Hệ thống kiểm tra từng sản phẩm trong đơn có tồn tại trong DB không |
| FR-03 | Kiểm tra tồn kho | Hệ thống kiểm tra số lượng tồn kho (SLTKho) có đủ đáp ứng số lượng yêu cầu không |

**Luồng xử lý:**
- **Thành công:** Phát `OrderSubmitSuccessResponse` kèm `NotificationPayLoad` -> Kích hoạt `ProcessOrderAcceptCommand` -> Chuyển tiếp `OrderAcceptConsumer`
- **Thất bại:** Phát `OrderSubmitFailedResponse` kèm `ErrorMessage` + `NotificationPayLoad`

### 2.2. Tính năng: Duyệt đơn hàng (Accept Order)

| ID | Yêu cầu | Mô tả |
|:---|:--------|:------|
| FR-04 | Tiếp nhận đơn hàng đã submit | `ProcessOrderAcceptCommand` kích hoạt `OrderAcceptConsumer` |
| FR-05 | Re-validate sản phẩm | Kiểm tra lại sản phẩm tồn tại và tồn kho lần cuối |
| FR-06 | Cập nhật trạng thái đơn hàng | Chuyển `OrderStatus` từ `Submitted` -> `Accepted` |
| FR-07 | Thiết lập Auto Timer | Tạo `OrderTimer` với thời gian timeout N phút, theo dõi tiến trình xử lý |
| FR-08 | Phát sự kiện Complete | Phát `OrderCompleteEvent` chứa danh sách Items để chuyển sang bước hoàn tất |

**Luồng xử lý:**
- **Thành công:** Phát `OrderAcceptedEvent` -> Phát `OrderCompleteEvent` (kích hoạt `OrderCompleteConsumer`) -> Phát `OrderAcceptSuccessResponse` kèm `NotificationPayLoad`
- **Thất bại:** Phát `OrderAcceptFailedResponse` kèm `ErrorReason` + `NotificationPayLoad`

### 2.3. Tính năng: Hoàn tất đơn hàng (Complete Order)

| ID | Yêu cầu | Mô tả |
|:---|:--------|:------|
| FR-09 | Tiếp nhận yêu cầu hoàn tất | `OrderCompleteEvent` kích hoạt `OrderCompleteConsumer` |
| FR-10 | Validate dữ liệu đơn hàng | Kiểm tra tính hợp lệ của đơn hàng lần cuối |
| FR-11 | Cập nhật trạng thái hoàn tất | Chuyển `OrderStatus` từ `Accepted` -> `Completed` |
| FR-12 | Trừ tồn kho | Giảm `SLTKho` của từng sản phẩm theo số lượng đã mua |

**Luồng xử lý:**
- **Thành công:** Phát `OrderCompleteSuccessResponse` kèm `NotificationPayLoad` -> Kết thúc Saga
- **Thất bại:** Phát `OrderCompleteFailedResponse` kèm `ErrorReason` + `NotificationPayLoad`

### 2.4. Tính năng: Auto Timeout xử lý đơn hàng

| ID | Yêu cầu | Mô tả |
|:---|:--------|:------|
| FR-13 | Tự động hết thời gian chờ | Sau N phút kể từ khi đơn hàng được duyệt, nếu chưa hoàn tất, `OrderAutoTimeoutExpiredEvent` được phát ra |
| FR-14 | Xử lý theo TargetAction | Dựa vào `TargetAction` trong event để thực hiện hành động tương ứng (Complete hoặc Reject) |

### 2.5. Tính năng: Real-time Notification

| ID | Yêu cầu | Mô tả |
|:---|:--------|:------|
| FR-15 | Thông báo real-time qua SignalR | Tất cả Response event (Success/Failed) đều chứa `NotificationPayLoad` để đẩy thông báo về UI |
| FR-16 | Notification Payload | Chứa `TargetUserId`, `Title`, `Message`, `NotificationType`, `Timestamp` |
| FR-17 | Lưu lịch sử thông báo | `Notification` entity lưu lại thông báo với trạng thái đã đọc/chưa đọc |

### 2.6. Tính năng: Lịch sử xử lý (Order History)

| ID | Yêu cầu | Mô tả |
|:---|:--------|:------|
| FR-18 | Ghi lại lịch sử các bước | Mỗi Consumer ghi lại `OrderHistory` với `ConsumerName`, `Status` (Success/Failed), `EventType`, `Message` |

---

## 3. SAGA FLOW (LUỒNG SAGA)

```
[UI] --(1)--> OrderSubmittedEvent
                |
                v
       [OrderSubmitConsumer]
          |              |
     (Success)       (Failed)
          |              |
          v              v
   ProcessOrderAcceptCommand  OrderSubmitFailedResponse (+SignalR)
          |
          v
   [OrderAcceptConsumer]
      |              |
  (Success)       (Failed)
      |              |
      v              v
  OrderCompleteEvent  OrderAcceptFailedResponse (+SignalR)
   + OrderAcceptedEvent
   + OrderAcceptSuccessResponse
      |
      v
  [OrderCompleteConsumer]
      |              |
  (Success)       (Failed)
      |              |
      v              v
  OrderCompleteSuccessResponse  OrderCompleteFailedResponse
      |                          (+SignalR)
      v
  (END - Kết thúc Saga)
      
  [Auto Timeout Job]
      |
      v
  OrderAutoTimeoutExpiredEvent
      |
      v
  Xử lý theo TargetAction (Complete/Reject)
```

---

## 4. CẤU TRÚC SỰ KIỆN (EVENT CATALOG)

### 4.1. Interface Base
| Interface | Thuộc tính |
|:----------|:-----------|
| **IOrderEvent** | `EventId`, `OrderId`, `Timestamp` |

### 4.2. Commands / Trigger Events
| Event | Dữ liệu | Mục đích |
|:------|:--------|:---------|
| **OrderSubmittedEvent** | EventId, OrderId, CustomerId, Items (List OrderItemDto), TotalAmount, Timestamp | Khởi tạo đơn hàng từ UI |
| **ProcessOrderAcceptCommand** | EventId, OrderId, CustomerId, Timestamp | Kích hoạt OrderAcceptConsumer sau Submit thành công |
| **OrderCompleteEvent** | EventId, OrderId, CustomerId, Items, Timestamp | Kích hoạt OrderCompleteConsumer để hoàn tất đơn |
| **OrderAutoTimeoutExpiredEvent** | EventId, OrderId, TargetAction, Timestamp | Tự động xử lý khi hết thời gian chờ |

### 4.3. Response Events (Success)
| Event | Dữ liệu | Mục đích |
|:------|:--------|:---------|
| **OrderSubmitSuccessResponse** | EventId, OrderId, CustomerId, Noti (NotificationPayLoad), Timestamp | Thông báo Submit thành công |
| **OrderAcceptedEvent** | EventId, OrderId, CustomerId, AutoTimeoutMinutes, Timestamp | Thông báo đơn hàng đã duyệt |
| **OrderAcceptSuccessResponse** | EventId, OrderId, CustomerId, Noti, Timestamp | Response duyệt thành công |
| **OrderCompleteSuccessResponse** | EventId, OrderId, CustomerId, Noti, Timestamp | Response hoàn tất thành công |

### 4.4. Response Events (Failed)
| Event | Dữ liệu | Mục đích |
|:------|:--------|:---------|
| **OrderSubmitFailedResponse** | EventId, OrderId, CustomerId, ErrorMessage, Noti, Timestamp | Thông báo Submit thất bại |
| **OrderAcceptFailedResponse** | EventId, OrderId, CustomerId, ErrorReason, Noti, Timestamp | Thông báo duyệt thất bại |
| **OrderCompleteFailedResponse** | EventId, OrderId, CustomerId, ErrorReason, Noti, Timestamp | Thông báo hoàn tất thất bại |

### 4.5. Notification Payload
```csharp
NotificationPayLoad(TargetUserId, Title, Message, NotificationType, Timestamp)
```

---

## 5. CẤU TRÚC DỮ LIỆU (DATA MODEL)

### 5.1. Order (Đơn hàng)
| Trường | Kiểu | Mô tả |
|:-------|:-----|:------|
| OrderId | Guid | Khóa chính |
| OrderCode | string | Mã đơn hàng |
| CustomerId | string | ID khách hàng |
| Status | OrderStatus (Enum) | Trạng thái: Submitted, Accepted, Completed, Rejected |
| TotalAmount | decimal | Tổng tiền |
| ShippingAddress | string | Địa chỉ giao hàng |
| Note | string | Ghi chú |
| UpdatedAt | DateTime? | Thời gian cập nhật |
| CompletedAt | DateTime? | Thời gian hoàn tất |
| RejectedAt | DateTime? | Thời gian từ chối |
| OrderItems | ICollection\<OrderItem> | Danh sách sản phẩm |

### 5.2. OrderItem (Sản phẩm trong đơn)
| Trường | Kiểu | Mô tả |
|:-------|:-----|:------|
| OrderItemId | Guid | Khóa chính |
| OrderId | Guid | FK -> Order |
| ProductId | Guid | ID sản phẩm |
| ProductName | string | Tên sản phẩm |
| Quantity | int | Số lượng |
| UnitPrice | decimal | Đơn giá |
| SubTotal | decimal (computed) | `Quantity * UnitPrice` |

### 5.3. Product (Sản phẩm)
| Trường | Kiểu | Mô tả |
|:-------|:-----|:------|
| ProductId | Guid | Khóa chính |
| Code | string | Mã sản phẩm |
| Name | string | Tên sản phẩm |
| SLTKho | int | Số lượng tồn kho |
| Price | decimal | Giá bán |
| IsActive | bool | Trạng thái kích hoạt |

### 5.4. OrderHistory (Lịch sử xử lý)
| Trường | Kiểu | Mô tả |
|:-------|:-----|:------|
| HistoryId | Guid | Khóa chính |
| OrderId | Guid | FK -> Order |
| ConsumerName | string | Tên Consumer xử lý |
| Status | HistoryStatus (Enum) | Success / Failed |
| EventType | string | Loại sự kiện |
| Message | string | Nội dung chi tiết |

### 5.5. OrderTimer (Hẹn giờ tự động)
| Trường | Kiểu | Mô tả |
|:-------|:-----|:------|
| TimerId | Guid | Khóa chính |
| OrderId | Guid | FK -> Order |
| Timeout | DateTime | Thời điểm timeout |
| Status | TargetStatus (Enum) | Completed / Rejected (hành động sẽ thực hiện khi timeout) |
| TimerStatus | TimerStatus (Enum) | Pending / Processed / Cancelled |

### 5.6. Notification (Thông báo)
| Trường | Kiểu | Mô tả |
|:-------|:-----|:------|
| NotifyId | Guid | Khóa chính |
| OrderId | Guid | FK -> Order |
| TargetUserId | Guid | ID người nhận trên UI |
| Title | string | Tiêu đề thông báo |
| Message | string | Nội dung |
| Type | string | Loại thông báo |
| IsRead | bool | Đã đọc? (default: false) |
| SendAt | DateTime | Thời gian gửi |

---

## 6. DANH SÁCH ENUMS

| Enum | Giá trị |
|:-----|:--------|
| **OrderStatus** | Submitted = 1, Accepted = 2, Completed = 3, Rejected = 4 |
| **HistoryStatus** | Success = 1, Failed = 2 |
| **TargetStatus** | Completed = 1, Rejected = 2 |
| **TimerStatus** | Pending = 1, Processed = 2, Cancelled = 3 |

---

## 7. YÊU CẦU PHI CHỨC NĂNG (NON-FUNCTIONAL REQUIREMENTS)

| ID | Yêu cầu | Mô tả |
|:---|:--------|:------|
| NFR-01 | Tính nhất quán (Eventually Consistent) | Saga Pattern đảm bảo dữ liệu cuối cùng sẽ nhất quán qua cơ chế bù trừ (compensating actions) khi có lỗi |
| NFR-02 | Tính sẵn sàng cao | Giao tiếp bất đồng bộ qua Message Broker giúp các service độc lập, không ảnh hưởng lẫn nhau |
| NFR-03 | Xử lý lỗi & Retry | Mỗi Consumer có cơ chế bắt lỗi và publish Failed Response tương ứng |
| NFR-04 | Real-time Updates | UI nhận được thông báo ngay lập tức khi có thay đổi trạng thái qua SignalR |
| NFR-05 | Audit Trail | Tất cả các bước xử lý đều được ghi lại trong OrderHistory để phục vụ debug và tracing |

---

## 8. SƠ ĐỒ KIẾN TRÚC

```
┌─────────────────────────────────────────────────────────┐
│                    UI / Client                           │
│                    (SignalR Client)                      │
└────────────────────┬────────────────────────────────────┘
                     │ OrderSubmittedEvent
                     ▼
┌─────────────────────────────────────────────────────────┐
│              Message Broker (RabbitMQ)                   │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐              │
│  │ Submit   │  │ Accept   │  │ Complete │              │
│  │ Queue    │  │ Queue    │  │ Queue    │              │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘              │
└───────┼──────────────┼──────────────┼──────────────────┘
        │              │              │
        ▼              ▼              ▼
┌──────────────┐ ┌──────────────┐ ┌────────────────┐
│OrderSubmit   │ │OrderAccept   │ │OrderComplete   │
│Consumer      │ │Consumer      │ │Consumer        │
│(Service)     │ │(Service)     │ │(Service)       │
└──────┬───────┘ └──────┬───────┘ └───────┬────────┘
       │                │                 │
       ▼                ▼                 ▼
┌─────────────────────────────────────────────────────────┐
│              SignalR Hub / Notification Service          │
│  Đẩy NotificationPayLoad về UI Client                    │
└─────────────────────────────────────────────────────────┘
```

---

## 9. DANH SÁCH SERVICE

| Service | Consumer | Xử lý | Đầu vào | Đầu ra (Success) | Đầu ra (Failed) |
|:--------|:---------|:------|:--------|:-----------------|:-----------------|
| **OrderSubmitService** | OrderSubmitConsumer | Kiểm tra sản phẩm & tồn kho | OrderSubmittedEvent | OrderSubmitSuccessResponse + ProcessOrderAcceptCommand | OrderSubmitFailedResponse |
| **OrderAcceptService** | OrderAcceptConsumer | Duyệt đơn, cập nhật trạng thái, cài timer | ProcessOrderAcceptCommand | OrderAcceptedEvent + OrderCompleteEvent + OrderAcceptSuccessResponse | OrderAcceptFailedResponse |
| **OrderCompleteService** | OrderCompleteConsumer | Hoàn tất đơn, trừ kho | OrderCompleteEvent | OrderCompleteSuccessResponse | OrderCompleteFailedResponse |

---

## 10. RÀNG BUỘC & GIẢ ĐỊNH

1. Mỗi đơn hàng phải có ít nhất 1 sản phẩm (OrderItem)
2. Số lượng tồn kho (SLTKho) phải >= số lượng yêu cầu để đơn hàng được chấp nhận
3. Mỗi sản phẩm phải có `IsActive = true` để được xử lý
4. Thời gian Auto Timeout được cấu hình trong hệ thống (N phút)
5. Tất cả Response event đều phải kèm `NotificationPayLoad` để đẩy real-time notification
