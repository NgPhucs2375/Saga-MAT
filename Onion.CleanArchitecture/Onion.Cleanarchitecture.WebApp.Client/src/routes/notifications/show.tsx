import React from "react";
import { useShow, useInvalidate, CanAccess } from "@refinedev/core";
import { Show, DateField } from "@refinedev/antd";
import {
  Card,
  Descriptions,
  Space,
  Button,
  Tag,
  Typography,
  App,
  Empty,
} from "antd";
import {
  CheckOutlined,
  BellOutlined,
  FileTextOutlined,
} from "@ant-design/icons";
import { INotification } from "./type";
import {
  NotificationTypeColor,
  NotificationTypeLabel,
} from "@components/notifications/notification-utils";

const { Text: TypographyText } = Typography;

export const ShowNoti: React.FC = () => {
  const {
    queryResult: { isLoading, data },
  } = useShow<INotification>();
  const noti = data?.data;
  const { message } = App.useApp();
  const invalidate = useInvalidate();

  const handleMarkAsRead = async () => {
    if (!noti) return;
    try {
      const res = await fetch(`/api/notifications/${noti.NotifyId}/read`, {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${localStorage.getItem("access_token") ?? ""}`,
        },
      });
      if (!res.ok) {
        message.error("Không thể đánh dấu đã đọc.");
        return;
      }
      message.success("Đã đánh dấu là đã đọc.");
      invalidate({ resource: "notifications", invalidates: ["detail", "list"] });
    } catch (err) {
      message.error((err as Error)?.message || "Thao tác thất bại.");
    }
  };

  return (
    <Show
      isLoading={isLoading}
      title={
        <Space>
          <BellOutlined />
          <TypographyText strong>Chi tiết thông báo</TypographyText>
        </Space>
      }
      headerButtons={
        noti && !noti.IsRead ? (
          <CanAccess
            resource="notifications"
            action="edit"
            params={{ id: noti.NotifyId }}
          >
            <Button
              type="primary"
              icon={<CheckOutlined />}
              onClick={handleMarkAsRead}
            >
              Đánh dấu đã đọc
            </Button>
          </CanAccess>
        ) : undefined
      }
    >
      {noti ? (
        <Card bordered={false}>
          <Space direction="vertical" size="large" style={{ width: "100%" }}>
            <Card
              size="small"
              style={{
                borderLeft: `4px solid ${
                  noti.IsRead ? "#52c41a" : "#faad14"
                }`,
                background: noti.IsRead ? "#f6ffed" : "#fffbe6",
              }}
            >
              <Space align="center" size={12}>
                <FileTextOutlined style={{ fontSize: 18 }} />
                <div>
                  <TypographyText strong style={{ fontSize: 16 }}>
                    {noti.Title}
                  </TypographyText>
                  <div>
                    <TypographyText type="secondary">{noti.Message}</TypographyText>
                  </div>
                </div>
              </Space>
            </Card>

            <Descriptions column={1} bordered size="middle">
              <Descriptions.Item label="Loại thông báo">
                <Tag color={NotificationTypeColor[noti.Type] ?? "default"}>
                  {NotificationTypeLabel[noti.Type] ?? noti.Type}
                </Tag>
              </Descriptions.Item>
              <Descriptions.Item label="Trạng thái">
                <Tag color={noti.IsRead ? "success" : "warning"}>
                  {noti.IsRead ? "Đã đọc" : "Chưa đọc"}
                </Tag>
              </Descriptions.Item>
              <Descriptions.Item label="Thời gian gửi">
                {noti.SendAt ? (
                  <DateField format="DD/MM/YYYY HH:mm" value={noti.SendAt} />
                ) : (
                  "-"
                )}
              </Descriptions.Item>
              <Descriptions.Item label="Mã đơn hàng liên kết">
                {noti.OrderId ? (
                  <TypographyText copyable>{noti.OrderId}</TypographyText>
                ) : (
                  "-"
                )}
              </Descriptions.Item>
              <Descriptions.Item label="Người nhận">
                <TypographyText copyable>{noti.TargetUserId}</TypographyText>
              </Descriptions.Item>
            </Descriptions>
          </Space>
        </Card>
      ) : (
        <Empty description="Không tìm thấy thông báo." />
      )}
    </Show>
  );
};
