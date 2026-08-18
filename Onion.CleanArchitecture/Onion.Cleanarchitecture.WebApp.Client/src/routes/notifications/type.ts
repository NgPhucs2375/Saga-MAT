export type NotiType = "Success" | "Error" | "Warning" | "Info";

export const NotiTypeLabel: Record<NotiType, string> = {
  Success: "Thành công",
  Error: "Thất bại",
  Warning: "Cảnh báo",
  Info: "Thông tin",
};

export interface INotification {
  NotifyId: string;
  OrderId: string;
  TargetUserId: string;
  Title: string;
  Message: string;
  Type: NotiType;
  IsRead: boolean;
  SendAt: string;
}

export interface INotificationDetail extends INotification {}

export interface ICreateNotification {
  OrderId: string;
  TargetUserId: string;
  Title: string;
  Message: string;
  Type: NotiType;
}
