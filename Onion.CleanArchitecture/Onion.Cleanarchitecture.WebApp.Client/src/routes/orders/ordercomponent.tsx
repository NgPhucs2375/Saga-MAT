import {
  Table,
  Typography,
  Descriptions,
  Card,
  Row,
  Col,
  Space,
  Statistic,
  theme,
  Empty,
  TableProps,
  Tag,
  Skeleton,
} from "antd";
import { DateField } from "@refinedev/antd";
import { DollarCircleOutlined, ShoppingCartOutlined } from "@ant-design/icons";
import { IOrderDetail, IOrderItem, OrderStatusLabel } from "./types";
import { ProcessSteps } from "@components/orders/process-steps";

interface OrderStatusTagProps {
  status: string; // Now directly takes the string label
}

export const OrderStatusTag: React.FC<OrderStatusTagProps> = ({ status }) => {
  let color;
  switch (status) {
    case "Submitted":
      color = "default"; // Màu xám
      break;
    case "Validating":
    case "Accepting":
    case "Completing":
    case "PendingApproval":
    case "Chờ duyệt":
      color = "processing"; // Màu xanh dương (đang xử lý)
      break;
    case "Completed":
      color = "success"; // Màu xanh lá
      break;
    case "Rejected":
    case "Cancelled":
    case "Compensating":
      color = "error"; // Màu đỏ
      break;
    default:
      color = "default";
      break;
  }

  return (
    <Tag color={color} style={{ textTransform: "capitalize" }}>
      {status.toLowerCase()}
    </Tag>
  );
};

const { Text: TypographyText } = Typography;

interface OrderShowContentProps {
  order?: IOrderDetail;
  isLoading: boolean;
}

export const OrderShowContent: React.FC<OrderShowContentProps> = ({
  order,
  isLoading,
}) => {
  const { token } = theme.useToken();

  const itemColumns: TableProps<IOrderItem>["columns"] = [
    {
      dataIndex: "ProductName",
      title: "Sản phẩm",
    },
    {
      dataIndex: "quantity",
      title: "SL",
      width: 80,
      align: "right",
    },
    {
      dataIndex: "price",
      title: "Đơn giá",
      width: 150,
      align: "right",
      render: (value: number) => (
        <TypographyText>{value?.toLocaleString() ?? 0} VND</TypographyText>
      ),
    },
    {
      key: "lineTotal",
      title: "Thành tiền",
      width: 150,
      align: "right",
      render: (_: unknown, item: IOrderItem) => (
        <TypographyText strong>
          {((item.Quantity ?? 0) * (item.UnitPrice ?? 0)).toLocaleString()} VND
        </TypographyText>
      ),
    },
  ];

  if (isLoading) {
    return <Skeleton active paragraph={{ rows: 10 }} />;
  }

  if (!order) {
    return <Empty description="Không tìm thấy thông tin đơn hàng." />;
  }

  return (
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
                  valueRender={() =>
                    order?.Status != null ? (
                      <OrderStatusTag
                        status={
                          OrderStatusLabel[order.Status] ?? String(order.Status)
                        }
                      />
                    ) : (
                      <TypographyText>-</TypographyText>
                    )
                  }
                />
              </Col>
              <Col xs={24} sm={8}>
                <Statistic
                  title="Tổng tiền"
                  value={order?.TotalAmount ?? 0}
                  precision={0}
                  formatter={(value) => (
                    <TypographyText strong style={{ color: token.colorPrimary }}>
                      {value?.toLocaleString()} VND
                    </TypographyText>
                  )}
                  prefix={<DollarCircleOutlined />}
                />
              </Col>
            </Row>
          </Card>

          <Card title="Thông tin chi tiết">
            <Descriptions bordered column={{ xs: 1, sm: 2, md: 3 }} size="small">
              <Descriptions.Item label="Địa chỉ giao hàng" span={3}>
                <TypographyText>{order?.ShippingAddress ?? "-"}</TypographyText>
              </Descriptions.Item>
              <Descriptions.Item label="Ghi chú" span={3}>
                <TypographyText>{order?.Note || "-"}</TypographyText>
              </Descriptions.Item>
              <Descriptions.Item label="Ngày tạo">
                {order?.Created ? (
                  <DateField format="DD/MM/YYYY HH:mm" value={order.Created} />
                ) : (
                  <TypographyText>-</TypographyText>
                )}
              </Descriptions.Item>
              <Descriptions.Item label="Ngày duyệt">
                {order?.UpdatedAt ? (
                  <DateField format="DD/MM/YYYY HH:mm" value={order.UpdatedAt} />
                ) : (
                  <TypographyText>-</TypographyText>
                )}
              </Descriptions.Item>
              <Descriptions.Item label="Hoàn tất">
                {order?.CompletedAt ? (
                  <DateField
                    format="DD/MM/YYYY HH:mm"
                    value={order.CompletedAt}
                  />
                ) : (
                  <TypographyText>-</TypographyText>
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
                      (sum, item) =>
                        sum + (item.Quantity ?? 0) * (item.UnitPrice ?? 0),
                      0
                    ) ?? 0;
                  return (
                    <Table.Summary.Row>
                      <Table.Summary.Cell index={0} colSpan={3}>
                        <TypographyText strong>Tổng tiền đơn hàng</TypographyText>
                      </Table.Summary.Cell>
                      <Table.Summary.Cell index={3}>
                        <TypographyText strong style={{ color: token.colorPrimary }}>
                          {total.toLocaleString()}
                        </TypographyText>
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
          <Card title="Lịch sử xử lý (Saga Process)">
            <ProcessSteps
              histories={order?.OrderHistories ?? []}
              orderStatus={order?.Status}
            />
          </Card>
        ) : (
          <Card title="Lịch sử xử lý (Saga Process)">
            <Empty description="Không có lịch sử xử lý nào." />
          </Card>
        )}
      </Col>
    </Row>
  );
};