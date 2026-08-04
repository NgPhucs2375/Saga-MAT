import dayjs from "dayjs";
import relativeTime from "dayjs/plugin/relativeTime";
import "dayjs/locale/vi";

dayjs.extend(relativeTime);
dayjs.locale("vi");

export const NotificationTypeColor: Record<string, string> = {
  Success: "green",
  Error: "red",
  Warning: "orange",
  Info: "blue",
};

export const NotificationTypeLabel: Record<string, string> = {
  Success: "Thành công",
  Error: "Thất bại",
  Warning: "Cảnh báo",
  Info: "Thông tin",
};

export const formatRelativeTime = (timestamp: string): string => {
  const date = dayjs(timestamp);
  const now = dayjs();
  const diffMinutes = now.diff(date, "minute");

  if (diffMinutes < 1) return "Vừa xong";
  if (diffMinutes < 60) return `${diffMinutes} phút trước`;

  const diffHours = now.diff(date, "hour");
  if (diffHours < 24) return `${diffHours} giờ trước`;

  const diffDays = now.diff(date, "day");
  if (diffDays < 7) return `${diffDays} ngày trước`;

  return date.format("DD/MM/YYYY HH:mm");
};