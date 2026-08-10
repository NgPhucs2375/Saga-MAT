import React from "react";
import { Card, Timeline, Typography, Spin, Empty } from "antd";
import { useList } from "@refinedev/core";
import { IOrderHistory } from "./types";
import {
  ClockCircleOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
} from "@ant-design/icons";

const { Text: TypographyText } = Typography;

enum HistoryStatus {
  Success = 1,
  Failed = 2,
}

export const SagaTimeline = ({ orderId }: { orderId: string }) => {
  // The IOrderHistory from types.ts is likely missing properties.
  // We extend it here to include the missing properties from the API response.
  type FullOrderHistory = IOrderHistory & { HistoryId: string; EventType: string; };
  const { data, isLoading, isError } = useList<FullOrderHistory>({
    resource: "orderhistories",
    filters: [{ field: "OrderId", operator: "eq", value: orderId }],
    sorters: [{ field: "CreatedAt", order: "asc" }],
    pagination: { pageSize: 100 },
    queryOptions: {
      enabled: !!orderId,
    },
  });

  const histories = data?.data ?? [];

  const getTimelineItem = (history: FullOrderHistory) => {
    let color: string;
    let icon: React.ReactNode;

    switch (history.Status) {
      case HistoryStatus.Success:
        color = "green";
        icon = <CheckCircleOutlined />;
        break;
      case HistoryStatus.Failed:
        color = "red";
        icon = <CloseCircleOutlined />;
        break;
      default:
        color = "blue";
        icon = <ClockCircleOutlined />;
        break;
    }

    return {
      key: history.HistoryId,
      color: color,
      dot: icon,
      children: (
        <>
          <TypographyText strong>{history.ConsumerName}</TypographyText>
          <TypographyText type="secondary" style={{ marginLeft: 8 }}>({history.EventType})</TypographyText>
          <p><TypographyText type="secondary">{history.Message}</TypographyText></p>
          <TypographyText style={{ fontSize: 12, color: "#999" }}>{new Date(history.CreatedAt).toLocaleString("vi-VN")}</TypographyText>
        </>
      ),
    };
  };

  return (
    <Card title="Giám sát Tiến trình Saga">
      {isLoading && <Spin />}
      {isError && <TypographyText type="danger">Không thể tải lịch sử.</TypographyText>}
      {!isLoading && !isError && (histories.length > 0 ? <Timeline items={histories.map(getTimelineItem)} /> : <Empty description="Không có lịch sử xử lý cho đơn hàng này." />)}
    </Card>
  );
}