import { useState, useMemo } from "react";
import {
  useTable,
  List,
  FilterDropdown,
  getDefaultSortOrder,
  getDefaultFilter,
} from "@refinedev/antd";
import { useInvalidate } from "@refinedev/core";
import { Button, Modal, Row, Col, Card, Empty, Space, Spin, Typography, Tooltip, App, Drawer, Table, Select, Input, theme } from "antd";
import {
  useNavigation,
  CanAccess,
  useDelete,
  useList,
} from "@refinedev/core";
import {
  EditOutlined,
  DeleteOutlined,
  ExclamationCircleOutlined,
  ShoppingCartOutlined,
  DollarCircleOutlined,
  ClockCircleOutlined,
  CheckCircleOutlined,
  EyeOutlined,
  CheckOutlined,
  CloseOutlined,
  PlusOutlined,
} from "@ant-design/icons";
import { IOrderDetail, OrderStatus, OrderStatusLabel } from "./types";
import { OrderShowContent, OrderStatusTag } from "./ordercomponent";
import "./orders.css";

const { Text: TypographyText } = Typography;

export const ListOrder = () => {
  const { token } = theme.useToken();
  const { tableProps, filters, sorters } = useTable<IOrderDetail>({
    resource: "orders",
    sorters: { initial: [{ field: "Created", order: "desc" }] },
  });
  const { message } = App.useApp();
  const invalidate = useInvalidate();

  const { create, edit, show } = useNavigation();
  const { mutate: deleteMutate } = useDelete();

  // State for drawer
  const [selectedOrder, setSelectedOrder] = useState<IOrderDetail | null>(null);
  const [drawerOpen, setDrawerOpen] = useState(false);

  // Fetch data for stats dashboard, respecting table filters
  const { data: statsData, isLoading: statsIsLoading } = useList<IOrderDetail>({
    resource: "orders",
    pagination: {
      mode: "off",
    },
    filters: filters,
    sorters: sorters,
  });

  const stats = useMemo(() => {
    if (!statsData || !statsData.data) {
      return {
        totalOrders: 0,
        totalRevenue: 0,
        submittedOrders: 0,
        pendingApprovalOrders: 0,
        completedOrders: 0,
      };
    }

    const orders = statsData.data;
    const totalRevenue = orders.reduce(
      (sum, order) => sum + order.TotalAmount,
      0
    );
    const submittedOrders = orders.filter(
      (o) => o.Status === OrderStatus.Submitted
    ).length;
    const pendingApprovalOrders = orders.filter(
      (o) => o.Status === OrderStatus.PendingApproval
    ).length;
    const completedOrders = orders.filter(
      (o) => o.Status === OrderStatus.Completed
    ).length;

    return {
      totalOrders: orders.length,
      totalRevenue,
      submittedOrders,
      pendingApprovalOrders,
      completedOrders,
    };
  }, [statsData]);

  const PENDING_APPROVAL_STATUS = OrderStatus.PendingApproval;

  const runApproveReject = async (id: string, action: "approve" | "reject", reason?: string) => {
    try {
      const opts: RequestInit = {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${localStorage.getItem("access_token") ?? ""}`,
        },
        body: action === "reject" && reason ? JSON.stringify({ reason }) : undefined,
      };
      const res = await fetch(`/api/orders/${id}/${action}`, opts);
      if (!res.ok) {
        const err = await res.json().catch(() => null);
        message.error(err?.message || "Thao tác thất bại.");
        return;
      }
      message.success(action === "approve" ? "Đã duyệt đơn hàng." : "Đã từ chối đơn hàng.");
      setDrawerOpen(false);
      setSelectedOrder(null);
      invalidate({ resource: "orders", invalidates: ["list", "many"] });
    } catch (e) {
      message.error((e as Error)?.message || "Thao tác thất bại.");
    }
  };

  const onApprove = (id: string) => runApproveReject(id, "approve");

  const onReject = (id: string) => {
    Modal.confirm({
      title: "Từ chối đơn hàng này?",
      content: "Hàng sẽ được mở khóa và bồi hoàn về trạng thái Rejected.",
      okText: "Từ chối",
      okButtonProps: { danger: true },
      cancelText: "Hủy",
      onOk: () => runApproveReject(id, "reject", "Người duyệt từ chối"),
    });
  };

  const showDeleteConfirm = (id: string, e: React.MouseEvent) => {
    e.stopPropagation();
    Modal.confirm({
      title: 'Bạn có chắc muốn xóa đơn hàng này?',
      icon: <ExclamationCircleOutlined />,
      content: 'Hành động này không thể hoàn tác.',
      okText: 'Xóa',
      cancelText: 'Hủy',
      onOk() {
        deleteMutate({ resource: "orders", id }, {
          onSuccess: () => {
            if (selectedOrder?.OrderId === id) {
              setDrawerOpen(false);
              setSelectedOrder(null);
            }
          }
        });
      },
    });
  };

  const handleRowClick = (record: IOrderDetail) => {
    setSelectedOrder(record);
    setDrawerOpen(true);
  };

  const columns = [
    {
      title: "Mã đơn",
      dataIndex: "OrderCode",
      key: "OrderCode",
      width: 160,
      sorter: true,
      defaultSortOrder: getDefaultSortOrder("OrderCode", sorters),
      defaultFilteredValue: getDefaultFilter("OrderCode", filters),
      filterDropdown: (props) => (
        <FilterDropdown {...props}>
          <Input placeholder="Tìm theo mã đơn..." allowClear style={{ minWidth: 220 }} />
        </FilterDropdown>
      ),
      render: (text: string) => (
        <Typography.Text strong style={{ color: token.colorPrimary }}>
          {text}
        </Typography.Text>
      ),
    },
    {
      title: "Khách hàng",
      dataIndex: "CustomerId",
      key: "CustomerId",
      width: 140,
      render: (value: string) => (
        <Typography.Text ellipsis>{value || "-"}</Typography.Text>
      ),
    },
    {
      title: "Trạng thái",
      dataIndex: "Status",
      key: "Status",
      width: 160,
      defaultFilteredValue: getDefaultFilter("Status", filters),
      filterDropdown: (props) => (
        <FilterDropdown {...props}>
          <Select
            style={{ minWidth: 220 }}
            placeholder="Lọc theo trạng thái..."
            allowClear
            options={Object.entries(OrderStatusLabel).map(([value, label]) => ({
              value,
              label,
            }))}
          />
        </FilterDropdown>
      ),
      render: (status: OrderStatus) => (
        <OrderStatusTag size="small" status={OrderStatusLabel[status] ?? String(status)} />
      ),
    },
    {
      title: "Tổng tiền",
      dataIndex: "TotalAmount",
      key: "TotalAmount",
      width: 160,
      align: "right",
      sorter: true,
      defaultSortOrder: getDefaultSortOrder("TotalAmount", sorters),
      render: (value: number) => <TypographyText strong>{value?.toLocaleString() ?? 0} VND</TypographyText>,
    },
    {
      title: "Ngày hoàn thành",
      dataIndex: "CompletedAt",
      key: "CompletedAt",
      width: 180,
      sorter: true,
      defaultSortOrder: getDefaultSortOrder("CompletedAt", sorters),
      render: (value: string) => value ? <TypographyText type="secondary">{new Date(value).toLocaleString("vi-VN")}</TypographyText> : <TypographyText>-</TypographyText>,
    },
    {
      title: "Thao tác",
      key: "actions",
      width: 200,
      fixed: "right",
      render: (_: unknown, record: IOrderDetail) => (
        <Space size={4}>
          <CanAccess resource="orders" action="show" params={{ id: record.OrderId }}>
            <Tooltip title="Xem chi tiết">
              <Button
                icon={<EyeOutlined />}
                size="small"
                onClick={(e) => {
                  e.stopPropagation();
                  show("orders", record.OrderId);
                }}
              />
            </Tooltip>
          </CanAccess>
          <CanAccess resource="orders" action="edit" params={{ id: record.OrderId }}>
            <Tooltip title="Chỉnh sửa">
              <Button
                icon={<EditOutlined />}
                size="small"
                onClick={(e) => {
                  e.stopPropagation();
                  edit("orders", record.OrderId);
                }}
              />
            </Tooltip>
          </CanAccess>
          <CanAccess resource="orders" action="delete" params={{ id: record.OrderId }}>
            <Tooltip title="Xóa">
              <Button
                icon={<DeleteOutlined />}
                size="small"
                danger
                onClick={(e) => showDeleteConfirm(record.OrderId, e)}
              />
            </Tooltip>
          </CanAccess>
          {record.Status === PENDING_APPROVAL_STATUS && (
            <>
              <Tooltip title="Duyệt đơn hàng">
                <Button
                  type="primary"
                  icon={<CheckOutlined />}
                  size="small"
                  onClick={(e) => {
                    e.stopPropagation();
                    onApprove(record.OrderId);
                  }}
                >
                  Duyệt
                </Button>
              </Tooltip>
              <Tooltip title="Từ chối đơn hàng">
                <Button
                  danger
                  icon={<CloseOutlined />}
                  size="small"
                  onClick={(e) => {
                    e.stopPropagation();
                    onReject(record.OrderId);
                  }}
                >
                  Từ chối
                </Button>
              </Tooltip>
            </>
          )}
        </Space>
      ),
    },
  ];

  return (
    <List
      headerButtons={
        <CanAccess resource="orders" action="create">
          <Button type="primary" onClick={() => create("orders")}>
            <PlusOutlined /> Tạo đơn hàng
          </Button>
        </CanAccess>
      }
    >
      {/* --- STATS DASHBOARD --- */}
      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        <Col xs={24} sm={12} md={6}>
          <StatCard
            title="Tổng số đơn hàng"
            value={stats.totalOrders}
            loading={statsIsLoading}
            icon={<ShoppingCartOutlined />}
            color={token.colorPrimary}
          />
        </Col>
        <Col xs={24} sm={12} md={6}>
          <StatCard
            title="Tổng doanh thu"
            value={stats.totalRevenue}
            loading={statsIsLoading}
            icon={<DollarCircleOutlined />}
            color={token.colorSuccess}
            suffix=" VND"
            formatter={(value) => value.toLocaleString()}
          />
        </Col>
        <Col xs={24} sm={12} md={6}>
          <StatCard
            title="Đơn chờ xử lý"
            value={stats.submittedOrders}
            loading={statsIsLoading}
            icon={<ClockCircleOutlined />}
            color={token.colorWarning}
          />
        </Col>
        <Col xs={24} sm={12} md={6}>
          <StatCard
            title="Chờ duyệt"
            value={stats.pendingApprovalOrders}
            loading={statsIsLoading}
            icon={<CheckCircleOutlined />}
            color={token.colorInfo}
          />
        </Col>
      </Row>

      {/* --- ORDER TABLE --- */}
      <Card>
        <Table
          {...tableProps}
          columns={columns}
          rowKey="OrderId"
          className="order-list-table"
          rowClassName={(record) => selectedOrder?.OrderId === record.OrderId ? "order-row-selected" : ""}
          onRow={(record) => ({
            onClick: () => handleRowClick(record),
            style: { cursor: "pointer" },
          })}
          pagination={{
            pageSize: 20,
            showSizeChanger: true,
            showQuickJumper: true,
            showTotal: (total) => `Tổng ${total} đơn hàng`,
          }}
          scroll={{ x: 1200 }}
        />
      </Card>

      {/* --- DETAIL DRAWER --- */}
      <Drawer
        title={selectedOrder ? `Chi tiết đơn hàng #${selectedOrder.OrderCode}` : "Chi tiết đơn hàng"}
        open={drawerOpen}
        onClose={() => { setDrawerOpen(false); setSelectedOrder(null); }}
        placement="right"
        width={720}
        extra={
          selectedOrder && (
            <Space>
              {selectedOrder.Status === PENDING_APPROVAL_STATUS && (
                <>
                  <Button type="primary" icon={<CheckOutlined />} onClick={() => onApprove(selectedOrder.OrderId)}>
                    Duyệt
                  </Button>
                  <Button danger icon={<CloseOutlined />} onClick={() => onReject(selectedOrder.OrderId)}>
                    Từ chối
                  </Button>
                </>
              )}
              <CanAccess resource="orders" action="show" params={{ id: selectedOrder.OrderId }}>
                <Tooltip title="Xem trang chi tiết">
                  <Button icon={<EyeOutlined />} onClick={() => show("orders", selectedOrder.OrderId)}>
                    Chi tiết
                  </Button>
                </Tooltip>
              </CanAccess>
              <CanAccess resource="orders" action="edit" params={{ id: selectedOrder.OrderId }}>
                <Button icon={<EditOutlined />} onClick={() => { setDrawerOpen(false); edit("orders", selectedOrder.OrderId); }}>
                  Chỉnh sửa
                </Button>
              </CanAccess>
              <CanAccess resource="orders" action="delete" params={{ id: selectedOrder.OrderId }}>
                <Button icon={<DeleteOutlined />} danger onClick={(e) => showDeleteConfirm(selectedOrder.OrderId, e)}>
                  Xóa
                </Button>
              </CanAccess>
            </Space>
          )
        }
      >
        {selectedOrder ? (
          <OrderShowContent isLoading={false} order={selectedOrder} />
        ) : (
          <Empty description="Chọn một đơn hàng để xem chi tiết" style={{ paddingTop: '100px' }} />
        )}
      </Drawer>
    </List>
  );
};

interface StatCardProps {
  title: string;
  value: number;
  loading?: boolean;
  icon: React.ReactNode;
  color: string;
  suffix?: string;
  formatter?: (value: number) => string;
}

const StatCard: React.FC<StatCardProps> = ({
  title,
  value,
  loading,
  icon,
  color,
  suffix,
  formatter,
}) => {
  return (
    <Card className="order-stat-card" styles={{ body: { padding: 20 } }}>
      <Space align="start" size={14} style={{ width: "100%" }}>
        <span
          className="order-header-ring"
          style={{
            width: 44,
            height: 44,
            fontSize: 20,
            color: "#fff",
            background: color,
            boxShadow: `0 6px 14px -4px ${color}66`,
          }}
        >
          {icon}
        </span>
        <div style={{ minWidth: 0 }}>
          <TypographyText type="secondary" style={{ fontSize: 13, lineHeight: "20px" }}>
            {title}
          </TypographyText>
          <div style={{ marginTop: 2 }}>
            {loading ? (
              <Spin size="small" />
            ) : (
              <TypographyText
                strong
                style={{ fontSize: 22, lineHeight: "28px", color: "rgba(0,0,0,0.88)" }}
              >
                {formatter ? formatter(value) : value.toLocaleString()}
                {suffix ? (
                  <TypographyText type="secondary" style={{ fontSize: 13, fontWeight: 500 }}>
                    {" "}{suffix}
                  </TypographyText>
                ) : null}
              </TypographyText>
            )}
          </div>
        </div>
      </Space>
    </Card>
  );
};