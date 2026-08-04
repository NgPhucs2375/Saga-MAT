import React, { useState } from "react";
import { Row, Col, Card, Select, Typography, Empty } from "antd";
import { useList } from "@refinedev/core";
import { IOrder } from "./types";

import { SagaTimeline } from "./saga-timeline";
import { LiveEventStream } from "./live-event-stream";
import { TimeoutManager } from "./timeout-manager";
import { ClientNotificationView } from "./client-notification-view";

const { Title } = Typography;

export const SagaMonitorDashboard = () => {
  const [selectedOrderId, setSelectedOrderId] = useState<string | null>(null);

  // Fetch all orders to populate the selector
  const { data: orders, isLoading: isLoadingOrders } = useList<IOrder>({
    resource: "orders",
    pagination: {
      pageSize: 1000, // Fetch a large number of orders for the selector
    },
    sorters: [
      {
        field: "CreatedAt",
        order: "desc",
      },
    ],
  });

  const orderOptions = orders?.data.map((order) => ({
    label: `Đơn hàng #${order.OrderCode} - ${new Date(
      order.CreatedAt
    ).toLocaleString("vi-VN")}`,
    value: order.OrderId,
  }));

  return (
    <>
      <Title level={2}>Bảng điều khiển giám sát Saga</Title>
      <Card style={{ marginBottom: 24 }}>
        <Select
          showSearch
          placeholder="Chọn một OrderId để giám sát"
          style={{ width: "100%" }}
          loading={isLoadingOrders}
          options={orderOptions}
          onSelect={(value) => setSelectedOrderId(value)}
          allowClear
          onClear={() => setSelectedOrderId(null)}
          filterOption={(input, option) =>
            (option?.label ?? "").toLowerCase().includes(input.toLowerCase())
          }
        />
      </Card>

      {selectedOrderId ? (
        <Row gutter={[16, 24]}>
          <Col span={24}>
            <SagaTimeline orderId={selectedOrderId} />
          </Col>
          <Col lg={16} xs={24}>
            <LiveEventStream correlationId={selectedOrderId} />
          </Col>
          <Col lg={8} xs={24}>
            <TimeoutManager orderId={selectedOrderId} />
            <ClientNotificationView orderId={selectedOrderId} />
          </Col>
        </Row>
      ) : (
        <Empty description="Vui lòng chọn một đơn hàng để xem chi tiết giám sát." />
      )}
    </>
  );
};
