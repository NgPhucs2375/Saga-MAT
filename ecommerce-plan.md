# Kế hoạch hoàn thiện E-Commerce: UI Admin + Customer Portal

> Project: **Demo-Saga** (Onion Clean Architecture + Refine/Antd + Saga/Outbox)
> Trạng thái: Đã chốt yêu cầu, chưa triển khai.

---

## 1. Bối cảnh & mục tiêu

Hệ thống hiện tại mới có **Admin SPA** (React + Refine + Antd) quản lý
products/orders/users/roles/notifications + dashboard saga (saga-monitor, saga-flow-diagram...).

Mục tiêu: biến hệ thống thành một **nền tảng e-commerce hoàn chỉnh** với:

- **Admin Portal** (giữ nguyên + bổ sung) — quản trị toàn bộ.
- **Customer Portal** (mới, SPA riêng) — cho khách hàng đặt đơn, theo dõi đơn, nhận thông báo realtime, xem profile.

### Các quyết định đã chốt

| Quyết định | Lựa chọn |
|---|---|
| "User" là ai | **Khách hàng đặt đơn** (CustomerId từ token `uid`) |
| Hình thức | **SPA/portal riêng biệt** (cross-origin + CORS) |
| Quyền customer trên đơn | **Chỉ xem / tạo / hủy đơn của chính mình** (không approve/reject) |
| Giỏ hàng | **Local state đơn giản** |
| Phạm vi đợt này | **Làm cả 3 phần**: Backend nền + Customer Portal + bổ sung Admin |

---

## 2. Hiện trạng hệ thống 

### Backend
- Stack: Onion Clean Architecture, CQRS + MediatR, MassTransit + RabbitMQ (saga), SignalR, Casbin (`policy.csv`).
- Roles enum: `SuperAdmin, Admin, Moderator, Basic` — **chưa có `Customer`**.
- `AccountService.GenerateJWToken` đóng các claim: `roles`, `uid`, `permission`, `Sub`, `Email`.
- `BaseApiController.EnforcePermissionAndExecute(resource, action)` → Casbin Enforce, throw 403 nếu thiếu quyền.
- Endpoints chính:
  - `api/orders` — list / show / create / cancel / approve / reject (mỗi cái gắn permission).
  - `api/orderhistories` — lịch sử saga (timeline).
  - `api/notifications` — list / read / show.
  - `api/account` — authenticate / me / register / forgot-password / reset-password.

### Phân quyền phía client
- Token lưu ở `localStorage["access_token"]`, gắn `Authorization: Bearer` khi gọi API.
- `auth-provider.getPermissions()` decode JWT tại chỗ (`jwt-decode`), không gọi API.
- `data-provider.ts` **chỉ fetch + gắn token**, KHÔNG kiểm tra quyền.
- Menu sider admin tự lọc theo `accessControlProvider.can()` + `resources.tsx`.

### Bảo mật đã đúng
- `GetAllOrderQueryHandler` + `GetOrderByIdQueryHandler`: non-admin chỉ thấy đơn của chính mình (so `CustomerId`).

### Lỗ hổng / thiếu sót cần xử lý
1. **Chưa có role `Customer`** trong `Roles.cs` + `policy.csv`.
2. **`CancelOrderCommand` KHÔNG kiểm tra sở hữu** — customer có quyền `orders/edit` có thể hủy/duyệt đơn người khác.
3. `ApproveOrderCommand` / `RejectOrderCommand` không chặn non-admin ở mức handler.
4. **Chưa có CORS** trong server.
5. `NotificationHub` đang `[AllowAnonymous]` — không map được user khi realtime.

---

## 3. Kiến trúc đích

```
Browser
  ├─ /5173  Admin SPA   (React + Refine + Antd)  ─┐
  └─ /5174  Customer SPA (Vite + React + Antd)   ─┤  CORS (cross-origin, AllowCredentials)
                                                   ▼
                                        .NET WebApp.Server  (api/*, /hubs/notification)
                                                   │
                                    ┌──────────────┴───────────────┐
                                    ▼                              ▼
                              SQL Server (EF)           MassTransit + RabbitMQ (saga)
```

- Admin gọi server qua **Vite proxy** (same-origin như cũ).
- Customer gọi **trực tiếp cross-origin** qua CORS (AllowCredentials, Origin cụ thể).
- SignalR: token truyền qua **query string** (browser không gửi header trong WebSocket), hub chặn anonymous.

---

## 4. Kế hoạch triển khai (3 phần)

### PHẦN A — Backend (nền cho cả 2 UI)

| # | File | Việc |
|---|---|---|
| A1 | `Application/Enums/Roles.cs` | Thêm `Customer` vào enum. |
| A2 | `WebApp.Server/wwwroot/policy.csv` | Thêm dòng Casbin cho `Customer`: `products list/show`; `orders list/show/create/cancel`; `notifications list/show`. |
| A3 | `Application/.../Commands/CancelOrder/CancelOrderCommand.cs` | **Sửa lỗ hổng**: inject `IAuthenticatedUserService`; non-SuperAdmin phải có `order.CustomerId == UserId` else 403. |
| A4 | `Application/.../Commands/ApproveOrder` + `RejectOrder` | Thêm chặn non-admin ở handler (phòng hờ). |
| A5 | `WebApp.Server/Program.cs` | Thêm `AddCors` (`WithOrigins(portalOrigin).AllowAnyHeader().AllowAnyMethod().AllowCredentials()`) + `app.UseCors()` **trước** `UseRouting()`. |
| A6 | `NotificationHub.cs` | Bỏ `[AllowAnonymous]`; `OnConnectedAsync` đọc token từ query string, subscribe theo `UserId`. |
| A7 | Xác minh query `GetOrderById` | Đảm bảo trả kèm `OrderItems` + `OrderHistories` cho màn chi tiết. |

### PHẦN B — Customer Portal (project mới `/CustomerPortal`)

**Stack:** Vite + React + TypeScript + Antd; giữ `jwt-decode`, `@microsoft/signalr`, `@refinedev/core`. Tái dùng backend `/api`.

| # | File | Việc |
|---|---|---|
| B1 | `vite.config.ts` | Proxy `/api` + `/hubs` sang server; port **5174**. |
| B2 | `providers/auth-provider.ts` | Login/register; lưu token `localStorage["access_token"]`; `getPermissions` = jwtDecode. |
| B3 | `providers/data-provider.ts` | Fetch `Bearer` token; chuẩn hóa `_start/_end` → API. |
| B4 | `App.tsx` + routes | Layout e-commerce: Header + menu (Sản phẩm, Giỏ hàng, Đơn của tôi, Thông báo, Profile). |
| B5 | `pages/Login.tsx`, `pages/Register.tsx` | Auth UI (register → role `Customer`). |
| B6 | `pages/Products.tsx` | Grid sản phẩm + nút "Thêm vào giỏ". |
| B7 | `pages/Cart.tsx` | Giỏ hàng local state → tạo đơn `POST /api/orders` (kèm `OrderItems`). |
| B8 | `pages/MyOrders.tsx` | `useList` orders (backend tự lọc CustomerId) + trạng thái. |
| B9 | `pages/OrderDetail.tsx` | `GetOrderById` + timeline `OrderHistories` + **SignalR** cập nhật realtime. |
| B10 | `components/NotificationBell.tsx` | Kết nối SignalR `/hubs/notification`, đếm unread, dropdown. |
| B11 | `pages/Profile.tsx` | `GET /api/account/me`, hiển thị/sửa thông tin. |

### PHẦN C — Admin Portal (bổ sung)

| # | File | Việc |
|---|---|---|
| C1 | `src/routes/` | Thêm màn **Danh sách khách hàng** (users filter role Customer) + xem đơn theo từng khách. |
| C2 | Dashboard | Tách rõ view thống kê admin (số đơn / trạng thái) bên cạnh saga-monitor. |
| C3 | Orders table | Nhóm nút Approve/Reject rõ ràng, chỉ hiện cho SuperAdmin/Admin. |
| C4 | `config/resources.tsx` | Đảm bảo role Customer không thấy menu admin (tự động theo `can()`). |

---

## 5. Giai đoạn & cách kiểm chứng

| Giai đoạn | Nội dung | Kiểm chứng |
|---|---|---|
| 1. Phần A | Backend: role Customer, CORS, sửa ownership, SignalR hub | `dotnet build`; login Customer; hủy đơn **của mình** ok, **của người khác** → 403. |
| 2. Phần B | Customer Portal | Portal chạy 5174; đăng ký → đặt đơn → xem realtime → profile. |
| 3. Phần C | Admin bổ sung | Admin SPA chạy song song; xem khách hàng & đơn của họ. |

---

## 6. Rủi ro & lưu ý

- **CORS + credentials**: `AllowAnyOrigin` KHÔNG dùng được với `AllowCredentials` — phải `WithOrigins(origin cụ thể)`.
- **SignalR cross-origin**: token phải qua query string; nhớ cấu hình `accessTokenFactory` phía client.
- **Saga/outbox**: khi sửa `CancelOrderCommand` không được phá vỡ idempotency guard đã có.
- **Đăng ký customer**: mặc định gán role `Customer` (đang là `Basic`) — cần chỉnh `AccountService.RegisterAsync`.

---

## 7. Cấu trúc thư mục đề xuất

```
Demo-Saga/
├─ Onion.CleanArchitecture/          # Backend (giữ nguyên, sửa theo Phần A)
├─ Onion.Cleanarchitecture.WebApp.Client/  # Admin SPA (Phần C)
├─ CustomerPortal/                   # MỚI - Customer SPA (Phần B)
├─ OrderSubmitService/ OrderAcceptService/ OrderCompleteService/ OrderOrchestration/...
└─ ecommerce-plan.md                 # Tài liệu này
```

---

*Tài liệu phục vụ lập kế hoạch và theo dõi tiến độ. Cập nhật trạng thái `[x]` khi hoàn thành từng mục.*
