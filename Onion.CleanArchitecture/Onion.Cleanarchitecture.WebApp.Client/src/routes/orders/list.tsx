import { useState, useMemo } from "react";
import {
  useTable,
  List,
} from "@refinedev/antd";
import { useUpdate, useInvalidate } from "@refinedev/core";
import { Button, Modal, Row, Col, Card, Statistic, Empty, Space, Spin, Typography, Tooltip, App, Drawer, Table } from "antd";
import {
  useNavigation,
  CanAccess,
  useOne,
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
  CheckOutlined,
  CloseOutlined,
  PlusOutlined,
} from "@ant-design/icons";
import { IOrderDetail, OrderStatus, OrderStatusLabel } from "./types";
import { OrderShowContent, OrderStatusTag } from "./ordercomponent";

const { Text: TypographyText } = Typography;

export const ListOrder = () => {
  const { tableProps, filters, sorters } = useTable<IOrderDetail>({
    resource: "orders",
    sorters: { initial: [{ field: "Created", order: "desc" }] },
  });
  const { message } = App.useApp();
  const invalidate = useInvalidate();

  const { create, edit } = useNavigation();
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
    } catch (e: any) {
      message.error(e?.message || "Thao tác thất bại.");
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
      render: (text: string) => <Typography.Text strong>{text}</Typography.Text>,
    },
    {
      title: "Khách hàng",
      dataIndex: "CustomerId",
      key: "CustomerId",
      width: 140,
    },
    {
      title: "Trạng thái",
      dataIndex: "Status",
      key: "Status",
      width: 140,
      render: (status: OrderStatus) => (
        <OrderStatusTag status={OrderStatusLabel[status] ?? String(status)} />
      ),
    },
    {
      title: "Tổng tiền",
      dataIndex: "TotalAmount",
      key: "TotalAmount",
      width: 160,
      align: "right",
      render: (value: number) => <TypographyText strong>{value?.toLocaleString() ?? 0} VND</TypographyText>,
    },
    {
      title: "Ngày tạo",
      dataIndex: "Created",
      key: "Created",
      width: 180,
      render: (value: string) => value ? <TypographyText type="secondary">{new Date(value).toLocaleString("vi-VN")}</TypographyText> : <TypographyText>-</TypographyText>,
    },
    {
      title: "Thao tác",
      key: "actions",
      width: 200,
      fixed: "right",
      render: (_: any, record: IOrderDetail) => (
        <Space size={4}>
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
          <Card>
            <Statistic
              title="Tổng số đơn hàng"
              value={stats.totalOrders}
              loading={statsIsLoading}
              prefix={<ShoppingCartOutlined />}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Card>
            <Statistic
              title="Tổng doanh thu"
              value={stats.totalRevenue}
              loading={statsIsLoading}
              prefix={<DollarCircleOutlined />}
              suffix="VND"
              formatter={(value) => value.toLocaleString()}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Card>
            <Statistic
              title="Đơn chờ xử lý"
              value={stats.submittedOrders}
              loading={statsIsLoading}
              prefix={<ClockCircleOutlined />}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Card>
            <Statistic
              title="Chờ duyệt"
              value={stats.pendingApprovalOrders}
              loading={statsIsLoading}
              prefix={<CheckCircleOutlined />}
            />
          </Card>
        </Col>
      </Row>

      {/* --- ORDER TABLE --- */}
      <Card>
        <Table
          {...tableProps}
          columns={columns}
          rowKey="OrderId"
          rowClassName={(record) => selectedOrder?.OrderId === record.OrderId ? "selected-row" : ""
}
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
              <CanAccess resource="orders" action="edit" params={{ id: selectedOrder.OrderId }}>
                <Button icon={<EditOutlined />} onClick={() => { setDrawerOpen(false); edit("orders", selectedOrder.OrderId); }}>
                  Chỉnh sửa
                </Button>
              </CanAccess>
              <CanAccess resource="orders" action="delete" params={{ id: selectedOrder.OrderId }}>
                <Button icon={<DeleteOutlined />} danger onClick={() => showDeleteConfirm(selectedOrder.OrderId, { stopPropagation: () => {} } as any)}>
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