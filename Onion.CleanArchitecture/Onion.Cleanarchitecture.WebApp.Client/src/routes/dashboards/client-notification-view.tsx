import React from "react";
import { Card, List, Avatar, Typography, Badge, Spin, Empty, Dropdown, Button } from "antd";
import { useList } from "@refinedev/core";
import { INotification } from "./types";
import { BellOutlined, CheckCircleOutlined, CloseCircleOutlined, InfoCircleOutlined, WarningOutlined } from "@ant-design/icons";

const { Text } = Typography;

export const ClientNotificationView = ({ orderId }: { orderId: string }) => {
  const { data, isLoading, isError } = useList<INotification>({
    resource: "notifications",
    filters: [{ field: "OrderId", operator: "eq", value: orderId }],
    sorters: [{ field: "SendAt", order: "desc" }],
    pagination: { pageSize: 10 },
    queryOptions: {
      enabled: !!orderId,
    },
  });

  const notifications = data?.data ?? [];
  const unreadCount = notifications.filter((n) => !n.IsRead).length;

  const getIcon = (type: INotification["Type"]) => {
    switch (type) {
      case "Success": return <CheckCircleOutlined style={{ color: "green" }} />;
      case "Error": return <CloseCircleOutlined style={{ color: "red" }} />;
      case "Warning": return <WarningOutlined style={{ color: "orange" }} />;
      default: return <InfoCircleOutlined style={{ color: "blue" }} />;
    }
  };

  const menu = (
    <List
      style={{ width: 350, backgroundColor: "white", boxShadow: "0 2px 8px rgba(0, 0, 0, 0.15)", borderRadius: 8 }}
      header={<div>Thông báo</div>}
      itemLayout="horizontal"
      dataSource={notifications}
      locale={{ emptyText: <Empty description="Không có thông báo" /> }}
      renderItem={(item) => (
        <List.Item style={{ padding: "12px 24px", backgroundColor: item.IsRead ? "transparent" : "#e6f7ff" }}>
          <List.Item.Meta
            avatar={<Avatar icon={getIcon(item.Type)} style={{ backgroundColor: 'transparent' }} />}
            title={<Text strong>{item.Title}</Text>}
            description={item.Message}
          />
        </List.Item>
      )}
    />
  );

  return (
    <Card title="Trải nghiệm Khách hàng (Notification)">
      <Text>Mô phỏng chuông thông báo và danh sách người dùng sẽ thấy.</Text>
      <div style={{ marginTop: 20, textAlign: "center" }}>
        <Dropdown overlay={menu} trigger={["click"]}>
          <Badge count={unreadCount}>
            <Button shape="circle" icon={<BellOutlined />} size="large" />
          </Badge>
        </Dropdown>
      </div>
      {isLoading && <Spin style={{ display: "block", marginTop: 16 }} />}
      {isError && <Text type="danger">Không thể tải thông báo.</Text>}
    </Card>
  );
};
