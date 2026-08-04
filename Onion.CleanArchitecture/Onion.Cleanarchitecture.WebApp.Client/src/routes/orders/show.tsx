import { useShow, useNavigation, useCan } from "@refinedev/core";
import { Show, DateField, EditButton, DeleteButton } from "@refinedev/antd";
import { Table, Tag, Typography, Descriptions, Timeline, Card, Row, Col, Space, Statistic, Tooltip, theme, Empty, TableProps } from "antd";
import {
  IOrderDetail,
  IOrderItem,
  IOrderHistory,
  OrderStatus,
  OrderStatusLabel,
  OrderStatusColor,
} from "./types";
import { DollarCircleOutlined, ShoppingCartOutlined, EditOutlined, DeleteOutlined } from "@ant-design/icons";

const { Text } = Typography;

export const ShowOrder = () => {
  const {
    queryResult: { isLoading, data },
  } = useShow<IOrderDetail>();

  const order = data?.data;
  const { token } = theme.useToken();

  const {  goBack } = useNavigation();

  // Check permissions for edit/delete
  const { data: canEdit } = useCan({
    resource: "orders",
    action: "edit",
    params: { record: order },
  });
  const { data: canDelete } = useCan({
    resource: "orders",
    action: "delete",
    params: { record: order },
  });

  const itemColumns: TableProps<IOrderItem>['columns'] = [
    {
      dataIndex: "ProductName",
      title: "Sản phẩm",    },
    {
      dataIndex: "quantity", // Corrected to lowercase 'quantity'
      title: "SL",
      width: 80,
      align: "right"
    },
    {
      dataIndex: "price", // Corrected to lowercase 'price'
      title: "Đơn giá",
      width: 150, // Use Text
      align: "right",
      render: (value: number) => <Text>{value?.toLocaleString() ?? 0} VND</Text>,
    },
    {
      key: "lineTotal",
      title: "Thành tiền",
      width: 150,
      align: "right",
      render: (_: unknown, item: IOrderItem) => // Changed record to item for clarity
        <Text strong>{((item.quantity ?? 0) * (item.price ?? 0)).toLocaleString()} VND</Text>,
    },
  ];

  return (
    <Show
      isLoading={isLoading}
      title={<Text>Chi tiết Đơn hàng #{order?.OrderCode}</Text>}
      headerButtons={
        <Space>
          {order?.Status === OrderStatus.Submitted && canEdit?.can && (
            <Tooltip title="Chỉnh sửa đơn hàng">
              <EditButton
                resource="orders"
                recordItemId={order?.OrderId}
                hideText
                icon={<EditOutlined />}
              />
            </Tooltip>
          )}
          {canDelete?.can && (
            <Tooltip title="Xóa đơn hàng">
              <DeleteButton
                resource="orders"
                recordItemId={order?.OrderId}
                hideText
                icon={<DeleteOutlined />}
                onSuccess={() => {
                  goBack();
                }}
              />
            </Tooltip>
          )}
        </Space>
      }
    >
      <Row gutter={[16, 16]}>
        <Col xl={16} lg={24} xs={24}>
          <Space direction="vertical" style={{ width: "100%" }} size="large">
            {/* Top section with key metrics */}
            <Card>
              <Row gutter={[16, 16]}>
                <Col xs={24} sm={8}>
                  <Statistic
                    title="Mã đơn hàng"
                    value={order?.OrderCode || "-"}
                    prefix={<ShoppingCartOutlined />}
                  />
                </Col>
                <Col xs={24} sm={8}>
                  <Statistic
                    title="Trạng thái"
                    valueRender={() => (
                      order?.Status != null ? (
                        <Tag color={OrderStatusColor[order.Status as OrderStatus]} style={{ fontSize: 16, }}>
                          {OrderStatusLabel[order.Status as OrderStatus] ?? order.Status}
                        </Tag>
                      ) : (
                        <Text>-</Text>
                      )
                    )}
                  />
                </Col>
                <Col xs={24} sm={8}>
                  <Statistic
                    title="Tổng tiền"
                    value={order?.TotalAmount ?? 0}
                    precision={0}
                    formatter={(value) => <Text strong style={{ color: token.colorPrimary }}>{value?.toLocaleString()} VND</Text>}
                    prefix={<DollarCircleOutlined />}
                  />
                </Col>
              </Row>
            </Card>

            <Card title="Thông tin chi tiết">
              <Descriptions bordered column={{ xs: 1, sm: 2, md: 3 }} size="small">
                <Descriptions.Item label="Địa chỉ giao hàng" span={3}>
                  <Text>{order?.ShippingAddress ?? "-"}</Text>
                </Descriptions.Item>
                <Descriptions.Item label="Ghi chú" span={3}>
                  <Text>{order?.Note || "-"}</Text>
                </Descriptions.Item>
                <Descriptions.Item label="Ngày tạo">
                  {order?.Created ? ( // Corrected to use 'order'
                    <DateField format="DD/MM/YYYY HH:mm" value={order.Created} />
                  ) : (
                    <Text>-</Text>
                  )}
                </Descriptions.Item>
                <Descriptions.Item label="Ngày duyệt">
                  {order?.UpdatedAt ? ( // Corrected to use 'order'
                    <DateField format="DD/MM/YYYY HH:mm" value={order.UpdatedAt} />
                  ) : (
                    <Text>-</Text>
                  )}
                </Descriptions.Item>
                <Descriptions.Item label="Hoàn tất">
                  {order?.CompletedAt ? (
                    <DateField format="DD/MM/YYYY HH:mm" value={order.CompletedAt} />
                  ) : (
                    <Text>-</Text>
                  )}
                </Descriptions.Item>
              </Descriptions>
            </Card>

            <Card title="Danh sách sản phẩm">
              {(order?.OrderItems?.length ?? 0) > 0 ? (
                <Table
                  dataSource={order?.OrderItems ?? []}
                  columns={itemColumns}
                  rowKey="OrderItemId"
                  pagination={false}
                  bordered
                  size="small"
                  summary={() => {
                    const total =
                      order?.OrderItems?.reduce(
                        (sum, item) => sum + (item.quantity ?? 0) * (item.price ?? 0),
                        0
                      ) ?? 0;
                    return (
                      <Table.Summary.Row>
                        <Table.Summary.Cell index={0} colSpan={3}>
                          <Text strong>Tổng tiền đơn hàng</Text>
                        </Table.Summary.Cell>
                        <Table.Summary.Cell index={3}>
                          <Text strong style={{ color: token.colorPrimary }}>{total.toLocaleString()}</Text>
                        </Table.Summary.Cell>
                      </Table.Summary.Row>
                    );
                  }}
                />
              ) : (
                <Empty description="Không có sản phẩm nào trong đơn hàng này." />
              )}
            </Card>
          </Space>
        </Col>

        <Col xl={8} lg={24} xs={24}>
          {(order?.OrderHistories?.length ?? 0) > 0 ? (
            <Card title="Lịch sử xử lý (Saga Timeline)">
              <Timeline
                items={(order?.OrderHistories ?? []).map((h: IOrderHistory) => {
                  const isSuccess = h.Status === 1;
                  const color = isSuccess ? "green" : "red";
                  const statusLabel = isSuccess ? "THÀNH CÔNG" : "THẤT BẠI";

                  return {
                    color: color,
                    children: (
                      <Space direction="vertical" style={{ gap: 2 }}>
                        <Space>
                          <Tag color={color}>{statusLabel}</Tag>
                          <Text strong>{h.ConsumerName}</Text>
                        </Space>
                        <Text type="secondary">{h.Message}</Text>
                        {h.CreatedAt && (
                          <DateField
                            style={{ fontSize: 12 }}
                            value={h.CreatedAt}
                            format="DD/MM/YYYY HH:mm:ss"
                          />
                        )}
                      </Space>
                    ),
                  };
                })}
              />
            </Card>
          ) : (
            <Card title="Lịch sử xử lý (Saga Timeline)">
              <Empty description="Không có lịch sử xử lý nào." />
            </Card>
          )}
        </Col>
      </Row>
    </Show>
  );
};