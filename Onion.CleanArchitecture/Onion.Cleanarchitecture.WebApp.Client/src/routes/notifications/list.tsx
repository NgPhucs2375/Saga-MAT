import React, { useMemo } from "react";
import { useTable, List } from "@refinedev/antd";
import {
  Table,
  Space,
  Button,
  Tag,
  Card,
  Tabs,
  Typography,
  App,
  Tooltip,
} from "antd";
import { useNavigation, useInvalidate, CanAccess } from "@refinedev/core";
import {
  EyeOutlined,
  CheckOutlined,
  BellOutlined,
  InfoCircleOutlined,
} from "@ant-design/icons";
import { INotification } from "./type";
import {
  NotificationTypeColor,
  NotificationTypeLabel,
} from "@components/notifications/notification-utils";

const { Text: TypographyText } = Typography;

export const ListNoti: React.FC = () => {
  const { tableProps, filters, setFilters } = useTable<INotification>({
    resource: "notifications",
    queryOptions: { refetchInterval: 5000 },
  });
  const { show } = useNavigation();
  const { message } = App.useApp();
  const invalidate = useInvalidate();

  const currentTab = useMemo(() => {
    const isReadFilter = filters.find(
      (f) => "field" in f && f.field === "IsRead"
    );
    if (isReadFilter && "value" in isReadFilter) {
      return String(isReadFilter.value);
    }
    return "all";
  }, [filters]);

  const handleTabChange = (key: string) => {
    if (key === "all") {
      setFilters(
        [{ field: "IsRead", operator: "eq", value: undefined }],
        "merge"
      );
    } else {
      setFilters(
        [{ field: "IsRead", operator: "eq", value: key === "true" }],
        "merge"
      );
    }
  };

  const handleMarkAsRead = async (id: string, e: React.MouseEvent) => {
    e.stopPropagation();
    try {
      const res = await fetch(`/api/notifications/${id}/read`, {
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
      invalidate({ resource: "notifications", invalidates: ["list"] });
    } catch (err) {
      message.error((err as Error)?.message || "Thao tác thất bại.");
    }
  };

  const columns = [
    {
      title: "Loại",
      dataIndex: "Type",
      key: "Type",
      width: 130,
      render: (type: string) => (
        <Tag color={NotificationTypeColor[type] ?? "default"}>
          {NotificationTypeLabel[type] ?? type}
        </Tag>
      ),
    },
    {
      title: "Tiêu đề",
      dataIndex: "Title",
      key: "Title",
      render: (text: string, record: INotification) => (
        <TypographyText strong={!record.IsRead}>{text}</TypographyText>
      ),
    },
    {
      title: "Nội dung",
      dataIndex: "Message",
      key: "Message",
      ellipsis: true,
    },
    {
      title: "Mã đơn",
      dataIndex: "OrderId",
      key: "OrderId",
      width: 180,
      render: (orderId: string) =>
        orderId ? (
          <TypographyText copyable>
            {orderId.substring(0, 8)}...
          </TypographyText>
        ) : (
          "-"
        ),
    },
    {
      title: "Trạng thái",
      dataIndex: "IsRead",
      key: "IsRead",
      width: 120,
      render: (isRead: boolean) => (
        <Tag color={isRead ? "success" : "warning"}>
          {isRead ? "Đã đọc" : "Chưa đọc"}
        </Tag>
      ),
    },
    {
      title: "Thời gian",
      dataIndex: "SendAt",
      key: "SendAt",
      width: 180,
      render: (value: string) =>
        value ? (
          <TypographyText type="secondary">
            {new Date(value).toLocaleString("vi-VN")}
          </TypographyText>
        ) : (
          "-"
        ),
    },
    {
      title: "Thao tác",
      key: "actions",
      width: 120,
      fixed: "right" as const,
      render: (_: unknown, record: INotification) => (
        <Space size={4}>
          <CanAccess
            resource="notifications"
            action="show"
            params={{ id: record.NotifyId }}
          >
            <Tooltip title="Xem chi tiết">
              <Button
                icon={<EyeOutlined />}
                size="small"
                onClick={() => show("notifications", record.NotifyId)}
              />
            </Tooltip>
          </CanAccess>
          {!record.IsRead && (
            <CanAccess
              resource="notifications"
              action="edit"
              params={{ id: record.NotifyId }}
            >
              <Tooltip title="Đánh dấu đã đọc">
                <Button
                  type="primary"
                  icon={<CheckOutlined />}
                  size="small"
                  onClick={(e) => handleMarkAsRead(record.NotifyId, e)}
                />
              </Tooltip>
            </CanAccess>
          )}
        </Space>
      ),
    },
  ];

  return (
    <List
      title={
        <Space>
          <BellOutlined />
          <TypographyText strong>Danh sách thông báo</TypographyText>
        </Space>
      }
    >
      <Card styles={{ body: { paddingTop: 8, paddingBottom: 0 } }}>
        <Tabs
          activeKey={currentTab}
          onChange={handleTabChange}
          items={[
            { key: "all", label: "Tất cả" },
            { key: "false", label: "Chưa đọc" },
            { key: "true", label: "Đã đọc" },
          ]}
        />
        <Table
          {...tableProps}
          columns={columns}
          rowKey="NotifyId"
          className="noti-list-table"
          onRow={(record) => ({
            onClick: () => show("notifications", record.NotifyId),
            style: { cursor: "pointer" },
          })}
          pagination={{
            pageSize: 10,
            showSizeChanger: true,
            showQuickJumper: true,
            showTotal: (total) => `Tổng ${total} thông báo`,
          }}
          scroll={{ x: 900 }}
          locale={{
            emptyText: (
              <Space direction="vertical" align="center" style={{ padding: 32 }}>
                <InfoCircleOutlined style={{ fontSize: 40, color: "#d9d9d9" }} />
                <TypographyText type="secondary">Không có thông báo</TypographyText>
              </Space>
            ),
          }}
        />
      </Card>
    </List>
  );
};
