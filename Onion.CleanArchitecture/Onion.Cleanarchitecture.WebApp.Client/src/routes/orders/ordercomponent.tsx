import {
  Table,
  Typography,
  Card,
  Row,
  Col,
  Space,
  theme,
  Empty,
  TableProps,
  Tag,
  Skeleton,
} from "antd";
import { DateField } from "@refinedev/antd";
import {
  DollarCircleOutlined,
  EnvironmentOutlined,
  FileTextOutlined,
  CalendarOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  ShoppingCartOutlined,
} from "@ant-design/icons";
import {
  IOrderDetail,
  IOrderItem,
  OrderStatusLabel,
  HistoryStatus,
} from "./types";
import { ProcessSteps } from "@components/orders/process-steps";
import "./orders.css";

const { Text: TypographyText } = Typography;

interface OrderStatusTagProps {
  status: string; // Now directly takes the string label
  size?: "small" | "large";
}

const STATUS_PRESET_HEX: Record<string, string> = {
  default: "#8c8c8c",
  processing: "#1677ff",
  success: "#52c41a",
  error: "#ff4d4f",
};

function resolveStatusPreset(label: string): string {
  switch (label) {
    case "Submitted":
      return "default";
    case "Validating":
    case "Accepting":
    case "Completing":
    case "PendingApproval":
    case "Chờ duyệt":
      return "processing";
    case "Completed":
      return "success";
    case "Rejected":
    case "Cancelled":
    case "Compensating":
      return "error";
    default:
      return "default";
  }
}

export const OrderStatusTag: React.FC<OrderStatusTagProps> = ({
  status,
  size = "large",
}) => {
  const preset = resolveStatusPreset(status);
  const dotColor = STATUS_PRESET_HEX[preset];

  return (
    <span style={{ display: "inline-flex", alignItems: "center" }} title={status}>
      <span
        className="order-status-dot"
        style={{
          backgroundColor: dotColor,
          width: size === "small" ? 6 : 8,
          height: size === "small" ? 6 : 8,
        }}
      />
      <Tag
        color={preset}
        style={{
          textTransform: "capitalize",
          marginInlineEnd: 0,
          fontSize: size === "small" ? 12 : 14,
          lineHeight: size === "small" ? "20px" : "24px",
          paddingInline: size === "small" ? 6 : 10,
        }}
      >
        {status.toLowerCase()}
      </Tag>
    </span>
  );
};

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
      title: "#",
      key: "index",
      width: 60,
      align: "center",
      render: (_: unknown, __: IOrderItem, index: number) => index + 1,
    },
    {
      title: "Sản phẩm",
      dataIndex: "ProductName",
      key: "ProductName",
      render: (value: string) => <TypographyText strong>{value}</TypographyText>,
    },
    {
      title: "SL",
      dataIndex: "Quantity",
      key: "Quantity",
      width: 90,
      align: "center",
      render: (value: number) => (
        <TypographyText>{value?.toLocaleString() ?? 0}</TypographyText>
      ),
    },
    {
      title: "Đơn giá",
      dataIndex: "UnitPrice",
      key: "UnitPrice",
      width: 140,
      align: "right",
      render: (value: number) => (
        <TypographyText>{value?.toLocaleString() ?? 0} VND</TypographyText>
      ),
    },
    {
      title: "Thành tiền",
      key: "lineTotal",
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

  const statusLabel =
    order.Status != null
      ? OrderStatusLabel[order.Status] ?? String(order.Status)
      : "Unknown";
  const accent = order.Status != null ? STATUS_PRESET_HEX[resolveStatusPreset(statusLabel)] : STATUS_PRESET_HEX.default;

  const itemsCount = order.OrderItems?.length ?? 0;
  const productCount =
    order.OrderItems?.reduce((sum, it) => sum + (it.Quantity ?? 0), 0) ?? 0;
  const totalAmount =
    order.OrderItems?.reduce(
      (sum, it) => sum + (it.Quantity ?? 0) * (it.UnitPrice ?? 0),
      0
    ) ?? order.TotalAmount ?? 0;

  const histories = order.OrderHistories ?? [];
  const failedCount = histories.filter(
    (h) => h.Status === HistoryStatus.Failed
  ).length;
  const successCount = histories.length - failedCount;

  const timeItems = [
    {
      icon: <CalendarOutlined />,
      label: "Ngày tạo",
      value: order.Created,
    },
    {
      icon: <CheckCircleOutlined />,
      label: "Ngày duyệt",
      value: order.UpdatedAt,
    },
    {
      icon: <DollarCircleOutlined />,
      label: "Hoàn tất",
      value: order.CompletedAt,
    },
    {
      icon: <CloseCircleOutlined />,
      label: "Từ chối",
      value: order.RejectedAt,
    },
  ];

  return (
    <Row gutter={[16, 16]}>
      <Col xl={16} lg={24} xs={24}>
        <Space direction="vertical" style={{ width: "100%" }} size="large">
          {/* --- HERO SECTION --- */}
          <Card
            className="order-hero"
            style={{
              borderLeft: `4px solid ${accent}`,
              background: `${accent}0d`,
            }}
          >
            <Row gutter={[16, 16]} align="middle">
              <Col xs={24} md={14}>
                <Space direction="vertical" size={6}>
                  <Space align="center" size={12} wrap>
                    <TypographyText strong style={{ fontSize: 20 }}>
                      #{order.OrderCode}
                    </TypographyText>
                    <OrderStatusTag status={statusLabel} size="large" />
                  </Space>
                  <Space size={16} wrap>
                    <TypographyText type="secondary" style={{ fontSize: 13 }}>
                      <CalendarOutlined style={{ marginRight: 4 }} />
                      {order.Created ? (
                        <DateField
                          format="DD/MM/YYYY HH:mm"
                          value={order.Created}
                        />
                      ) : (
                        "–"
                      )}
                    </TypographyText>
                    <TypographyText type="secondary" style={{ fontSize: 13 }}>
                      <ShoppingCartOutlined style={{ marginRight: 4 }} />
                      {productCount} sản phẩm · {itemsCount} loại
                    </TypographyText>
                  </Space>
                </Space>
              </Col>
              <Col xs={24} md={10}>
                <div style={{ textAlign: "right" }}>
                  <TypographyText
                    type="secondary"
                    style={{ fontSize: 13, display: "block", marginBottom: 2 }}
                  >
                    Tổng tiền
                  </TypographyText>
                  <TypographyText
                    strong
                    style={{ fontSize: 26, color: token.colorPrimary }}
                  >
                    <DollarCircleOutlined style={{ marginRight: 8 }} />
                    {(order.TotalAmount ?? totalAmount).toLocaleString()} VND
                  </TypographyText>
                </div>
              </Col>
            </Row>
          </Card>

          {/* --- SHIPPING INFO --- */}
          <Card title="Thông tin giao hàng">
            <Space direction="vertical" size="middle" style={{ width: "100%" }}>
              <Space align="start" size={12}>
                <EnvironmentOutlined
                  style={{ fontSize: 16, color: token.colorPrimary, marginTop: 2 }}
                />
                <div>
                  <TypographyText
                    type="secondary"
                    style={{ fontSize: 12, display: "block" }}
                  >
                    Địa chỉ giao hàng
                  </TypographyText>
                  <TypographyText strong>
                    {order.ShippingAddress ?? "–"}
                  </TypographyText>
                </div>
              </Space>
              <Space align="start" size={12}>
                <FileTextOutlined
                  style={{ fontSize: 16, color: token.colorPrimary, marginTop: 2 }}
                />
                <div>
                  <TypographyText
                    type="secondary"
                    style={{ fontSize: 12, display: "block" }}
                  >
                    Ghi chú
                  </TypographyText>
                  <TypographyText>{order.Note || "–"}</TypographyText>
                </div>
              </Space>
            </Space>
          </Card>

          {/* --- TIMELINE --- */}
          <Card title="Thời gian xử lý">
            <Row gutter={[16, 16]}>
              {timeItems.map((item) => (
                <Col xs={12} lg={6} key={item.label}>
                  <div>
                    <span
                      style={{
                        color: token.colorPrimary,
                        marginRight: 6,
                        fontSize: 13,
                      }}
                    >
                      {item.icon}
                    </span>
                    <TypographyText type="secondary" style={{ fontSize: 12 }}>
                      {item.label}
                    </TypographyText>
                    <div style={{ marginTop: 4 }}>
                      {item.value ? (
                        <DateField
                          format="DD/MM/YYYY HH:mm"
                          value={item.value}
                        />
                      ) : (
                        <TypographyText type="secondary">–</TypographyText>
                      )}
                    </div>
                  </div>
                </Col>
              ))}
            </Row>
          </Card>

          {/* --- ORDER ITEMS --- */}
          <Card title={`Danh sách sản phẩm${itemsCount > 0 ? ` (${itemsCount})` : ""}`}>
            {itemsCount > 0 ? (
              <Table
                dataSource={order.OrderItems ?? []}
                columns={itemColumns}
                rowKey="OrderItemId"
                pagination={false}
                size="small"
                className="order-item-table"
                scroll={{ x: "max-content", y: 320 }}
                summary={() => (
                  <Table.Summary.Row>
                    <Table.Summary.Cell index={0} colSpan={4}>
                      <TypographyText strong>Tổng tiền đơn hàng</TypographyText>
                    </Table.Summary.Cell>
                    <Table.Summary.Cell index={4}>
                      <TypographyText
                        strong
                        style={{ color: token.colorPrimary }}
                      >
                        {totalAmount.toLocaleString()} VND
                      </TypographyText>
                    </Table.Summary.Cell>
                  </Table.Summary.Row>
                )}
              />
            ) : (
              <Empty description="Không có sản phẩm nào trong đơn hàng này." />
            )}
          </Card>
        </Space>
      </Col>

      <Col xl={8} lg={24} xs={24}>
        <Card
          title="Lịch sử xử lý (Saga)"
          extra={
            histories.length > 0 ? (
              <Space size={4}>
                <Tag color="success">
                  <CheckCircleOutlined /> {successCount}
                </Tag>
                <Tag color="error" style={{ marginInlineEnd: 0 }}>
                  <CloseCircleOutlined /> {failedCount}
                </Tag>
              </Space>
            ) : undefined
          }
        >
          {histories.length > 0 ? (
            <ProcessSteps
              histories={order.OrderHistories ?? []}
              orderStatus={order.Status}
            />
          ) : (
            <Empty description="Không có lịch sử xử lý nào." />
          )}
        </Card>
      </Col>
    </Row>
  );
};