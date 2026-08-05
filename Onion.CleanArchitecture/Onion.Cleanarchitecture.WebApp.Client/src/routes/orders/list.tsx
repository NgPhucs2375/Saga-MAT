import { useState, useMemo } from "react";
import {
  useTable,
  List,
  getDefaultSortOrder,
  DateField,
  FilterDropdown,
} from "@refinedev/antd";
import { Table, Input, Button, Select, DatePicker, Drawer, Dropdown, MenuProps, Modal, Row, Col, Card, Statistic } from "antd";
import {
  getDefaultFilter,
  useNavigation,
  CanAccess,
  useShow,
  useCan,
  useDelete,
  useList,
} from "@refinedev/core";
import {
  MoreOutlined,
  EyeOutlined,
  EditOutlined,
  DeleteOutlined,
  ExclamationCircleOutlined,
  ShoppingCartOutlined,
  DollarCircleOutlined,
  ClockCircleOutlined,
  CheckCircleOutlined,
} from "@ant-design/icons";
import { IOrderDetail, OrderStatus, OrderStatusLabel } from "./types";
import { PaginationTotal } from "@components/pagination-total";
import { OrderShowContent, OrderStatusTag } from "./ordercomponent";

const OrderActions = ({ record, showDrawer }: { record: IOrderDetail, showDrawer: (id: string) => void }) => {
  const { edit } = useNavigation();
  const { mutate: deleteMutate } = useDelete();

  const { data: canEdit } = useCan({ resource: "orders", action: "edit", params: { id: record.OrderId } });
  const { data: canDelete } = useCan({ resource: "orders", action: "delete", params: { id: record.OrderId } });

  const showDeleteConfirm = (id: string) => {
    Modal.confirm({
      title: 'Bạn có chắc muốn xóa đơn hàng này?',
      icon: <ExclamationCircleOutlined />,
      content: 'Hành động này không thể hoàn tác.',
      okText: 'Xóa',
      cancelText: 'Hủy',
      onOk() {
        deleteMutate({ resource: "orders", id });
      },
    });
  };

  const menuItems: MenuProps["items"] = [
    {
      key: "show",
      label: "Xem chi tiết",
      icon: <EyeOutlined />,
      onClick: () => showDrawer(record.OrderId),
    },
  ];

  if (record.Status === OrderStatus.Submitted && canEdit?.can) {
    menuItems.push({
      key: "edit",
      label: "Chỉnh sửa",
      icon: <EditOutlined />,
      onClick: () => edit("orders", record.OrderId),
    });
  }

  if (canDelete?.can) {
    menuItems.push({ key: "divider", type: "divider" });
    menuItems.push({
      key: "delete",
      label: "Xóa",
      icon: <DeleteOutlined />,
      danger: true,
      onClick: () => showDeleteConfirm(record.OrderId),
    });
  }

  return (
    <Dropdown menu={{ items: menuItems }} trigger={["click"]}>
      <Button type="text" icon={<MoreOutlined />} />
    </Dropdown>
  );
};

export const ListOrder = () => {
  const { tableProps, sorters, filters } = useTable<IOrderDetail>({
    resource: "orders",
    pagination: { current: 1, pageSize: 10 },
    sorters: { initial: [{ field: "Created", order: "desc" }] },
  });

  const { create } = useNavigation();

  // Fetch data for stats dashboard, respecting table filters
  const { data: statsData, isLoading: statsIsLoading } = useList<IOrderDetail>({
    resource: "orders",
    pagination: {
      mode: "off", // Fetch all records matching filters
    },
    filters: filters,
  });

  const stats = useMemo(() => {
    if (!statsData?.data) {
      return {
        totalOrders: 0,
        totalRevenue: 0,
        submittedOrders: 0,
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
    const completedOrders = orders.filter(
      (o) => o.Status === OrderStatus.Completed
    ).length;

    return {
      totalOrders: orders.length,
      totalRevenue,
      submittedOrders,
      completedOrders,
    };
  }, [statsData]);

  // State for Drawer
  const [drawerVisible, setDrawerVisible] = useState(false);
  const [recordId, setRecordId] = useState<string | null>(null);

  // Hook to fetch data for the drawer
  const { queryResult } = useShow<IOrderDetail>({
    resource: "orders",
    id: recordId ?? "",
    queryOptions: {
      enabled: !!recordId,
    },
  });

  const showDrawer = (id: string) => {
    setRecordId(id);
    setDrawerVisible(true);
  };

  return (
    <List
      headerButtons={
        <>
          <CanAccess resource="orders" action="create">
            <Button type="primary" onClick={() => create("orders")}>
              Tạo đơn hàng
            </Button>
          </CanAccess>
        </>
      }
    >
      {/* Stats Dashboard Section */}
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
              title="Đơn hàng chờ xử lý"
              value={stats.submittedOrders}
              loading={statsIsLoading}
              prefix={<ClockCircleOutlined />}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Card>
            <Statistic
              title="Đơn hàng hoàn tất"
              value={stats.completedOrders}
              loading={statsIsLoading}
              prefix={<CheckCircleOutlined />}
            />
          </Card>
        </Col>
      </Row>
      <Table
        {...tableProps}
        rowKey="OrderId"
        pagination={{
          ...tableProps.pagination,
          showTotal: (total) => (
            <PaginationTotal total={total} entityName="đơn hàng" />
          ),
        }}
      >
        {/* Mã đơn */}
        <Table.Column
          dataIndex="OrderCode"
          title="Mã đơn"
          sorter
          defaultSortOrder={getDefaultSortOrder("OrderCode", sorters)}
          defaultFilteredValue={getDefaultFilter("OrderCode", filters)}
          filterDropdown={(props) => (
            <FilterDropdown {...props}>
              <Input placeholder="Tìm mã đơn..." />
            </FilterDropdown>
          )}
          width={180}
        />

        {/* Trạng thái */}
        <Table.Column
          dataIndex="Status"
          title="Trạng thái"
          sorter
          width={140}
          render={(value: OrderStatus) => (
            <OrderStatusTag status={OrderStatusLabel[value] ?? String(value)} />
          )}
          defaultFilteredValue={getDefaultFilter("Status", filters)}
          filterDropdown={(props) => (
            <FilterDropdown {...props}>
              <Select
                style={{ minWidth: 150 }}
                placeholder="Lọc trạng thái..."
                allowClear
              >
                {Object.entries(OrderStatusLabel).map(([key, label]) => (
                  <Select.Option key={key} value={parseInt(key)}>
                    {label}
                  </Select.Option>
                ))}
              </Select>
            </FilterDropdown>
          )}
        />

        {/* Tổng tiền */}
        <Table.Column
          dataIndex="TotalAmount"
          title="Tổng tiền"
          sorter
          width={150}
          render={(value: number) => (
            <span style={{ fontWeight: 600 }}>
              {value?.toLocaleString() ?? 0} VND
            </span>
          )}
        />

        {/* Địa chỉ */}
        <Table.Column
          dataIndex="ShippingAddress"
          title="Địa chỉ giao hàng"
          ellipsis
          width={220}
          defaultFilteredValue={getDefaultFilter("ShippingAddress", filters)}
          filterDropdown={(props) => (
            <FilterDropdown {...props}>
              <Input placeholder="Tìm địa chỉ..." />
            </FilterDropdown>
          )}
        />

        {/* Ngày tạo */}
        <Table.Column
          dataIndex="Created"
          title="Ngày tạo"
          render={(value) => (
            <DateField format="DD/MM/YYYY HH:mm" value={value} />
          )}
          defaultFilteredValue={getDefaultFilter("Created", filters, "between")}
          filterDropdown={(props) => (
            <FilterDropdown {...props}>
              <DatePicker.RangePicker />
            </FilterDropdown>
          )}
          sorter
          defaultSortOrder={getDefaultSortOrder("Created", sorters)}
          width={180}
        />

        {/* Hoàn tất lúc */}
        <Table.Column
          dataIndex="CompletedAt"
          title="Hoàn tất lúc"
          render={(value) =>
            value ? <DateField format="DD/MM/YYYY HH:mm" value={value} /> : "-"
          }
          width={180}
        />

        {/* Actions */}
        <Table.Column
          title="Thao tác"
          fixed="right"
          width={100}
          align="center"
          render={(_, record: IOrderDetail) => <OrderActions record={record} showDrawer={showDrawer} />}
        />
      </Table>
      <Drawer
        open={drawerVisible}
        onClose={() => setDrawerVisible(false)}
        width="50%"
        title={`Chi tiết Đơn hàng #${queryResult.data?.data?.OrderCode ?? ""}`}
      >
        <OrderShowContent
          isLoading={queryResult.isLoading}
          order={queryResult.data?.data}
        />
      </Drawer>
    </List>
  );
};