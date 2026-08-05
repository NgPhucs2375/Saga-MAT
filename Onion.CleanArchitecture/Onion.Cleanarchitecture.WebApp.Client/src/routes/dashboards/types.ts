import { OrderStatus, OrderStatusLabel, IOrderHistory } from "@routes/orders/types";

// Define IOrder here, as it's not exported from the central orders/types
export interface IOrder {
  OrderId: string;
  OrderCode: string;
  Status: OrderStatus;
  TotalAmount: number;
  CreatedAt: string;
  // Optional customer info based on old dashboard
  customer?: {
    id: string;
    fullName: string;
  };
}

export { OrderStatus, OrderStatusLabel };
export type { IOrderHistory };

export enum EventStoreStatus {
  Pending = 0,
  Processed = 1,
  Failed = 2,
}

export interface IEventStore {
  StoreId: string;
  CorrelationId: string;
  EventType: string;
  PayLoad: string;
  Status: EventStoreStatus;
  ErrorMessage?: string;
  RetryCount: number;
  SequenceNumber: number;
  ProcessedAt?: string;
  CreatedAt: string;
}

export enum TimerStatus {
  Pending = 1,
  Processed = 2,
  Cancelled = 3,
}

export interface IOrderTimer {
  TimerId: string;
  OrderId: string;
  Timeout: string;
  Status: OrderStatus;
  TimerStatus: TimerStatus;
}

export interface INotification {
  NotifyId: string;
  OrderId: string;
  TargetUserId: string;
  Title: string;
  Message: string;
  Type: "Success" | "Error" | "Info" | "Warning";
  IsRead: boolean;
  SendAt: string;
}
