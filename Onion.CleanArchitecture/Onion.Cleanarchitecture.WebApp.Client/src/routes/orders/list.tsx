import {
  useTable,
  List,
  ShowButton,
  DeleteButton,
  getDefaultSortOrder,
  DateField,
  FilterDropdown,
} from "@refinedev/antd";
import { Table, Space, Input, Tag, Button, Select, DatePicker } from "antd";
import {
  getDefaultFilter,
  useNavigation,
  CanAccess,
} from "@refinedev/core";
import { IOrder, OrderStatus, OrderStatusLabel, OrderStatusColor } from "./types";
import { PaginationTotal } from "@components/pagination-total";

export const ListOrder = () => {
  const { tableProps, sorters, filters } = useTable<IOrder>({
    resource: "orders",
    pagination: { current: 1, pageSize: 10 },
    sorters: { initial: [{ field: "Created", order: "desc" }] },
  });

  const { create } = useNavigate();

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
            <Tag color={OrderStatusColor[value]}>
              {OrderStatusLabel[value] ?? value}
            </Tag>
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
          render={(_, record: IOrder) => (
            <Space>
              <ShowButton hideText size="small" recordItemId={record.OrderId} />
              <DeleteButton hideText size="small" recordItemId={record.OrderId} />
            </Space>
          )}
        />
      </Table>
    </List>
  );
};