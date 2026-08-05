export enum OrderStatus {
  Pending = 0,
  Submitted = 1,
  Accepted = 2,
  Rejected = 3,
  Completed = 4,
  Cancelled = 5,
}

export const OrderStatusLabel: Record<OrderStatus, string> = {
  [OrderStatus.Pending]: "Chờ xử lý",
  [OrderStatus.Submitted]: "Đã gửi",
  [OrderStatus.Accepted]: "Đã chấp nhận",
  [OrderStatus.Rejected]: "Đã từ chối",
  [OrderStatus.Completed]: "Hoàn thành",
  [OrderStatus.Cancelled]: "Đã hủy",
};

export const OrderStatusColor: Record<OrderStatus, string> = {
  [OrderStatus.Pending]: "blue",
  [OrderStatus.Submitted]: "orange",
  [OrderStatus.Accepted]: "green",
  [OrderStatus.Rejected]: "red",
  [OrderStatus.Completed]: "purple",
  [OrderStatus.Cancelled]: "gray",
};

export interface IProduct {
  ProductId: string;
  Name: string;
  Price: number;
  AvailableQty: number;
}

export interface IOrderItem {
  id?: string; // Optional for new items, present for existing
  ProductId: string;
  Quantity: number;
  ProductName?: string; // Used in ShowOrder.tsx, might be denormalized
  UnitPrice?: number; // Alias for price, if backend sends it this way
}

export interface ICreateOrder {
  Items: IOrderItem[]; // Matches backend CreateOrderCommand: ProductId + Quantity
  ShippingAddress?: string;
  Note?: string;
}

export interface IOrderHistory {
  id?: string;
  Status: number; // Assuming number maps to OrderStatus
  ConsumerName: string;
  Message: string;
  CreatedAt: string; // ISO date string
}

export interface IOrderDetail {
  OrderId: string;
  OrderCode: string;
  CustomerId?: string;
  TotalAmount: number;
  Status: OrderStatus;
  ShippingAddress?: string;
  Note?: string;
  Created: string; // Corrected from CreatedAt based on error message
  UpdatedAt?: string;
  CompletedAt?: string;
  OrderItems: IOrderItem[]; // Used in ShowOrder.tsx
  OrderHistories?: IOrderHistory[];
}