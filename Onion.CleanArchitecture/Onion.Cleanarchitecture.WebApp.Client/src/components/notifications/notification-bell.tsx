import React from "react";
import { Badge, Button, Dropdown, Space, Typography } from "antd";
import { BellOutlined, CheckOutlined, DeleteOutlined } from "@ant-design/icons";
import { useNotifications } from "./notification-context-model";
import { NotificationList } from "./notification-list";

export const NotificationBell: React.FC = () => {
  const { notifications, unreadCount, markRead, markAllRead, clear } =
    useNotifications();

  return (
    <Dropdown
      trigger={["click"]}
      dropdownRender={() => (
        <div
          style={{
            width: 380,
            background: "#fff",
            borderRadius: 8,
            boxShadow: "0 6px 16px rgba(0,0,0,0.15)",
            overflow: "hidden",
          }}
        >
          {/* Header */}
          <div
            style={{
              padding: "12px 16px",
              borderBottom: "1px solid #f0f0f0",
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
            }}
          >
            <Typography.Text strong>Thông báo</Typography.Text>
            <Space size={4}>
              {unreadCount > 0 && (
                <Button
                  type="text"
                  size="small"
                  icon={<CheckOutlined />}
                  onClick={markAllRead}
                >
                  Đã đọc
                </Button>
              )}
              {notifications.length > 0 && (
                <Button
                  type="text"
                  size="small"
                  danger
                  icon={<DeleteOutlined />}
                  onClick={clear}
                >
                  Xóa
                </Button>
              )}
            </Space>
          </div>

          {/* Notifications */}
          <NotificationList
            notifications={notifications}
            onItemRead={markRead}
            maxHeight={400}
          />
        </div>
      )}
    >
      <Badge count={unreadCount} size="small" offset={[0, 0]}>
        <BellOutlined
          style={{ fontSize: 18, cursor: "pointer", color: "#666" }}
        />
      </Badge>
    </Dropdown>
  );
};