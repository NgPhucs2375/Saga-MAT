// === Order Status Enum ===
export enum OrderStatus {
  Submitted = 1,
  Accepted = 2,
  Completed = 3,
  Rejected = 4,
}

// === Order Status Display ===
export const OrderStatusLabel: Record<OrderStatus, string> = {
  [OrderStatus.Submitted]: "Đã gửi",
  [OrderStatus.Accepted]: "Đã duyệt",
  [OrderStatus.Completed]: "Hoàn tất",
  [OrderStatus.Rejected]: "Từ chối",
};

export const OrderStatusColor: Record<OrderStatus, string> = {
  [OrderStatus.Submitted]: "blue",
  [OrderStatus.Accepted]: "orange",
  [OrderStatus.Completed]: "green",
  [OrderStatus.Rejected]: "red",
};

// === Order Item (FE) ===
export interface IOrderItem {
  OrderItemId: string;
  OrderId: string;
  ProductId: string;
  ProductName: string;
  Quantity: number;
  UnitPrice: number;
}

// === Order (FE - từ GET /api/orders) ===
export interface IOrder {
  OrderId: string;
  OrderCode: string;
  CustomerId: string;
  Status: OrderStatus;
  TotalAmount: number;
  ShippingAddress: string;
  Note: string;
  CreatedAt: string;
  UpdatedAt?: string;
  CompletedAt?: string;
  RejectedAt?: string;
  OrderItems: IOrderItem[];
}

// === Show Order response (từ GET /api/orders/show/{id}) ===
export interface IOrderDetail extends IOrder {
  OrderHistories?: IOrderHistory[];
}

export interface IOrderHistory {
  HistoryId: string;
  OrderId: string;
  ConsumerName: string;
  Status: number; // HistoryStatus: Success=1, Failed=2
  EventType: string;
  Message: string;
  CreatedAt: string;
}

// === Create Order (FE -> POST /api/orders) ===
export interface ICreateOrderItem {
  ProductId: string;
  Quantity: number;
}

export interface ICreateOrder {
  ShippingAddress: string;
  Note?: string;
  Items: ICreateOrderItem[];
}