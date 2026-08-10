export enum OrderStatus {
  Submitted = 1,
  Accepted = 2,
  Completed = 3,
  Rejected = 4,
  PendingApproval = 5,
  Cancelled = 6,
  Compensating = 7,
}

export const OrderStatusLabel: Record<OrderStatus, string> = {
  [OrderStatus.Submitted]: "Submitted",
  [OrderStatus.Accepted]: "Accepted",
  [OrderStatus.Completed]: "Completed",
  [OrderStatus.Rejected]: "Rejected",
  [OrderStatus.PendingApproval]: "Chờ duyệt",
  [OrderStatus.Cancelled]: "Cancelled",
  [OrderStatus.Compensating]: "Compensating",
};

export enum HistoryStatus {
  Success = 1,
  Failed = 2,
}

export interface IOrderItem {
  OrderItemId: string;
  OrderId: string;
  ProductId: string;
  ProductName: string;
  Quantity: number;
  UnitPrice: number;
  SubTotal: number; // Computed, but good to have in interface
}

export interface IOrderHistory {
  HistoryId: string;
  OrderId: string;
  ConsumerName: string;
  Status: HistoryStatus;
  EventType: string;
  Message: string;
  CreatedAt: string; // Assuming string for DateField
}

export interface IOrderDetail {
  OrderId: string;
  OrderCode: string;
  CustomerId: string;
  Status: OrderStatus;
  TotalAmount: number;
  ShippingAddress: string;
  Note?: string;
  Created: string; // Assuming string for DateField
  UpdatedAt?: string;
  CompletedAt?: string;
  RejectedAt?: string;
  OrderItems?: IOrderItem[];
  OrderHistories?: IOrderHistory[];
  Items?: ICreateOrderItem[]; // Added for form mapping in edit mode
}

export interface ICreateOrderItem {
  ProductId: string;
  Quantity: number;
}

export interface ICreateOrder {
  ShippingAddress: string;
  Note?: string;
  Items: ICreateOrderItem[]; // Renamed from OrderItems to Items as per backend payload
}

export interface IProduct {
  /** Unique identifier for the product. */
  ProductId: string;
  /** The product's stock keeping unit (SKU) or other code. */
  Code: string;
  /** The display name of the product. */
  Name: string;
  /**
   * The physical quantity of the product in stock.
   * Vietnamese: "Số Lượng Tồn Kho".
   */
  SLTKho: number;
  /** The quantity reserved for pending orders. */
  ReservedQty: number;
  /** The quantity available for sale (PhysicalQty - ReservedQty). */
  AvailableQty: number;
  /** The price of a single unit of the product. */
  Price: number;
  /** Indicates if the product is active and can be ordered. */
  IsActive: boolean;
  /** Optional URL for the product's image. */
  ImageUrl?: string;
}