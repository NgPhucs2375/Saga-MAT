import { Empty, List, Tag, Typography } from "antd";
import {
  NotificationTypeColor,
  NotificationTypeLabel,
  formatRelativeTime,
} from "./notification-utils";
import type { StoredNotification } from "./notification-context";

interface NotificationListProps {
  notifications: StoredNotification[];
  onItemRead?: (id: string) => void;
  maxHeight?: number;
  emptyText?: string;
  renderItem?: (item: StoredNotification) => React.ReactNode;
}

export const NotificationList: React.FC<NotificationListProps> = ({
  notifications,
  onItemRead,
  maxHeight = 400,
  emptyText = "Chưa có thông báo",
  renderItem,
}) => {
  if (notifications.length === 0) {
    return (
      <Empty
        image={Empty.PRESENTED_IMAGE_SIMPLE}
        description={emptyText}
        style={{ padding: "24px 0" }}
      />
    );
  }

  return (
    <List
      dataSource={notifications}
      style={{ maxHeight, overflow: "auto" }}
      renderItem={(item) =>
        renderItem ? (
          <div key={item.id} onClick={() => onItemRead?.(item.id)}>
            {renderItem(item)}
          </div>
        ) : (
          <List.Item
            key={item.id}
            onClick={() => onItemRead?.(item.id)}
            style={{
              cursor: "pointer",
              background: item.read ? undefined : "#e6f4ff",
              padding: "12px 16px",
              borderBottom: "1px solid #f0f0f0",
            }}
          >
            <List.Item.Meta
              title={
                <span style={{ display: "flex", alignItems: "center", gap: 8 }}>
                  <Tag
                    color={
                      NotificationTypeColor[item.NotificationType] ?? "default"
                    }
                  >
                    {NotificationTypeLabel[item.NotificationType] ??
                      item.NotificationType}
                  </Tag>
                  {!item.read && <Tag color="blue">Mới</Tag>}
                </span>
              }
              description={
                <div>
                  <Typography.Text
                    style={{ fontWeight: item.read ? 400 : 600 }}
                  >
                    {item.Title}
                  </Typography.Text>
                  <div style={{ marginTop: 4, color: "#666" }}>
                    {item.Message}
                  </div>
                </div>
              }
            />
            <Typography.Text
              type="secondary"
              style={{ fontSize: 12, whiteSpace: "nowrap" }}
            >
              {formatRelativeTime(item.Timestamp)}
            </Typography.Text>
          </List.Item>
        )
      }
    />
  );
};