export enum OrderStatus {
  Submitted = 1,
  Accepted = 2,
  Completed = 3,
  Rejected = 4,
}

export const OrderStatusLabel: Record<OrderStatus, string> = {
  [OrderStatus.Submitted]: "Đã gửi",
  [OrderStatus.Accepted]: "Đã chấp nhận",
  [OrderStatus.Completed]: "Hoàn thành",
  [OrderStatus.Rejected]: "Đã từ chối",
};

export const OrderStatusColor: Record<OrderStatus, string> = {
  [OrderStatus.Submitted]: "orange",
  [OrderStatus.Accepted]: "green",
  [OrderStatus.Completed]: "purple",
  [OrderStatus.Rejected]: "red",
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