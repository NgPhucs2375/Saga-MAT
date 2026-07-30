# TÀI LIỆU KIẾN TRÚC VÀ QUY TRÌNH XỬ LÝ ĐƠN HÀNG (ORDER PROCESSING FLOW)

---

## 1. TỔNG QUAN LUỒNG XỬ LÝ (OVERVIEW FLOW)

Hệ thống xử lý đơn hàng được thiết kế theo mô hình **Microservices** và kiến trúc **Clean Architecture**, giao tiếp bất đồng bộ qua **Event-Driven Architecture** (Message Broker) và cập nhật thời gian thực tới UI qua **SignalR**.

```
[UI / Client] 
     │ (1. Request Tạo đơn)
     ▼
[Order Service] ────► Publish OrderSubmittedEvent
                             │
                             ▼
                  [OrderSubmitConsumer]
                             │
                             ▼
                  [OrderAcceptConsumer]
                             │
                             ▼
                  [OrderCompleteConsumer]
                             │
                             ▼
                    [SignalR Hub / UI]
```

---

## 2. CHI TIẾT CÁC CONSUMER VA LOGIC XỬ LÝ

### 2.1. OrderSubmitConsumer
* **Nhiệm vụ:** Kiểm tra điều kiện tiên quyết của sản phẩm khi khởi tạo đơn hàng.
* **Các bước xử lý:**
  1. Kiểm tra sản phẩm có tồn tại trong hệ thống.
  2. Kiểm tra số lượng sản phẩm tồn tại trong kho có đủ đáp ứng nhu cầu.
* **Kết quả:**
  * **Nếu thành công (Pass):** Publish `OrderSubmitSuccessResponse` -> Chuyển tiếp tới step `OrderAcceptConsumer`.
  * **Nếu thất bại (Fail):** Publish `OrderSubmitFailedResponse` (Trả lỗi).

---

### 2.2. OrderAcceptConsumer
* **Nhiệm vụ:** Duyệt đơn hàng và thiết lập thời gian tự động xử lý.
* **Các bước xử lý:**
  1. Kiểm tra lại thông tin sản phẩm (tương tự bước 1).
  2. Cập nhật trạng thái đơn hàng (Order Status).
  3. Cài đặt hẹn giờ tự động (Auto-timer): Sau **N phút**, nếu không có người duyệt/thao tác, đơn hàng sẽ tự động **Reject** hoặc **Complete**.
* **Kết quả:**
  * **Nếu không phát sinh lỗi validate / lỗi khác:**
    * Publish `OrderCompleteEvent` (chuyển tiếp tới step `OrderCompleteConsumer`).
    * Publish `OrderAcceptSuccessResponse`.
  * **Nếu thất bại:** Publish `OrderAcceptFailedResponse` (Trả lỗi).

---

### 2.3. OrderCompleteConsumer
* **Nhiệm vụ:** Hoàn tất đơn hàng và trừ kho sản phẩm.
* **Các bước xử lý:**
  1. Validate lại toàn bộ dữ liệu đơn hàng.
  2. Cập nhật trạng thái đơn hàng thành hoàn tất.
  3. Trừ số lượng sản phẩm trong kho.
* **Kết quả:**
  * **Nếu không phát sinh lỗi validate / lỗi khác:** Publish `OrderCompleteSuccessResponse`.
  * **Nếu thất bại:** Publish `OrderCompleteFailedResponse` (Trả lỗi).

---

## 3. TÍCH HỢP SIGNALR NOTIFICATION FOR UI

Tất cả các sự kiện Response (`Success` hoặc `Failed`) được phát ra từ từng Consumer trong quy trình đều đẩy thông báo qua **SignalR Hub** để giao diện người dùng (UI) nhận thông tin theo thời gian thực (Real-time Notification).

* **Mô hình gửi thông báo:**
  * Consumer Process Result -> SignalR Notification Service / Hub -> Client UI (Receive Notification).

---

## 4. CẤU TRÚC KIẾN TRÚC CLEAN ARCHITECTURE

Mỗi Microservice (ví dụ: `Order.Service`) tuân thủ nghiêm ngặt 4 lớp của Clean Architecture:

```
Order.Service /
│
├── Core /
│   ├── Domain /
│   │   ├── Entities /         # Order, OrderItem
│   │   ├── Enums /            # OrderStatus (Submitted, Accepted, Completed, Rejected)
│   │   └── Events /           # OrderSubmittedEvent, OrderAcceptedEvent, OrderCompletedEvent
│   │
│   └── Application /
│       ├── Interfaces /       # IOrderRepository, ISignalRNotificationService, IInventoryService
│       ├── Commands /         # CreateOrderCommand, AcceptOrderCommand, CompleteOrderCommand
│       └── Dtos /             # OrderDto, NotificationDto
│
├── Infrastructure /
│   ├── Persistence /          # DbContext, Repository implementations
│   ├── Messaging /            # Message Broker setup (RabbitMQ/MassTransit)
│   ├── ExternalServices /     # Inventory API Client
│   └── SignalR /              # SignalR Hub implementation & notification dispatcher
│
└── Presentation / (Consumers)
    ├── OrderSubmitConsumer.cs
    ├── OrderAcceptConsumer.cs
    └── OrderCompleteConsumer.cs
```

---

## 5. TỔNG KẾT QUY TRÌNH LUỒNG DỮ LIỆU (DATA FLOW SUMMARY)

| Tên Consumer | Nhiệm vụ chính | Hành động Success | Hành động Failed |
| :--- | :--- | :--- | :--- |
| **OrderSubmitConsumer** | Kiểm tra sản phẩm & tồn kho | Publish Success Response -> Trigger Accept | Publish Error Response |
| **OrderAcceptConsumer** | Re-validate, đổi trạng thái, cài Timer N phút | Publish `OrderCompleteEvent` + Success Response | Publish Error Response |
| **OrderCompleteConsumer** | Re-validate, trừ tồn kho, hoàn tất | Publish Success Response | Publish Error Response |
| **SignalR Integration** | Lắng nghe các Response Event | Đẩy Notification trực tiếp về UI Client | Đẩy Notification trực tiếp về UI Client |