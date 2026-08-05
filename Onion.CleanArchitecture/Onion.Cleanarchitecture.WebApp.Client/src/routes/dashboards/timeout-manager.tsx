import { Card, Statistic, Typography, Tag, Spin, Empty } from "antd";
import { useList } from "@refinedev/core";
import { IOrderTimer, TimerStatus, OrderStatusLabel } from "./types";
import { ClockCircleOutlined } from "@ant-design/icons";

const { Countdown } = Statistic;
const { Text } = Typography;

export const TimeoutManager = ({ orderId }: { orderId: string }) => {
  const { data, isLoading, isError } = useList<IOrderTimer>({
    resource: "ordertimers",
    filters: [{ field: "OrderId", operator: "eq", value: orderId }],
    sorters: [{ field: "Timeout", order: "desc" }],
    pagination: { pageSize: 5 },
    queryOptions: {
      enabled: !!orderId,
    },
  });

  const timers = data?.data ?? [];

  const getTimerStatusTag = (status: TimerStatus) => {
    switch (status) {
      case TimerStatus.Pending: return <Tag color="processing">Active</Tag>;
      case TimerStatus.Processed: return <Tag color="success">Processed</Tag>;
      case TimerStatus.Cancelled: return <Tag color="default">Stopped</Tag>;
      default: return <Tag>Unknown</Tag>;
    }
  };

  return (
    <Card title="Quản lý Thời gian chờ (OrderTimer)">
      {isLoading && <Spin />}
      {isError && <Text type="danger">Không thể tải timers.</Text>}
      {!isLoading && !isError && (timers.length > 0 ? timers.map((timer) => (
        <Card key={timer.TimerId} type="inner" style={{ marginBottom: 16 }}>
          <Countdown
            title={<><ClockCircleOutlined /> Thời gian chờ còn lại</>}
            value={timer.Timeout}
            format="HH:mm:ss"
            valueStyle={{ color: "#cf1322" }}
          />
          <div style={{ marginTop: 16 }}>
            <Text strong>Trạng thái Timer: </Text>
            {getTimerStatusTag(timer.TimerStatus)}
          </div>
          <div>
            <Text strong>Hành động khi hết giờ: </Text>
            <Text type="warning">{OrderStatusLabel[timer.Status] ?? "Không xác định"}</Text>
          </div>
        </Card>
      )) : (
        <Empty description="Không có timer nào đang hoạt động cho đơn hàng này." />
      ))}
    </Card>
  );
};
