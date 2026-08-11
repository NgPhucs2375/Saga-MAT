import {
  useTable,
  List,
  getDefaultSortOrder,
  DateField,
  FilterDropdown,
  useSelect,
  ExportButton,
} from "@refinedev/antd";
import {
  Table,
  Input,
  Button,
  DatePicker,
  Select,
  Row,
  Col,
  Card,
  Statistic,
  Space,
  Typography,
} from "antd";
import { IProduct } from "./types";
import {
  getDefaultFilter,
  useNavigation,
  useDeleteMany,
  useMany,
  useList,
  useExport,
  CanAccess,
} from "@refinedev/core";
import React, { useMemo } from "react";
import { PaginationTotal } from "@components/pagination-total";
import { IUser } from "@routes/identity/users";
import {
  ProductActions,
  ProductStatusTag,
  StockLevelTag,
  ProductThumb,
} from "./productcomponent";
import {
  ShoppingCartOutlined,
  DollarOutlined,
  WarningOutlined,
} from "@ant-design/icons";

const formatCurrency = (value: number) =>
  `${Number(value || 0).toLocaleString("vi-VN")} ₫`;

const StatCard = ({
  title,
  value,
  loading,
  icon,
  color,
  formatter,
}: {
  title: string;
  value: number;
  loading?: boolean;
  icon: React.ReactNode;
  color: string;
  formatter: (value: number | string) => string;
}) => (
  <Card hoverable bordered={false} style={{ height: "100%" }}>
    <Space size={16} align="start">
      <div
        style={{
          width: 48,
          height: 48,
          borderRadius: 12,
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          fontSize: 22,
          flexShrink: 0,
          background: `${color}1a`,
          color,
        }}
      >
        {icon}
      </div>
      <Statistic title={title} value={value} loading={loading} formatter={formatter} />
    </Space>
  </Card>
);

export const ListProduct = () => {
  const { mutate: deleteMutate } = useDeleteMany();
  const { tableProps, sorters, filters } = useTable<IProduct>({
    resource: "products",
    pagination: { current: 1, pageSize: 10 },
    sorters: { initial: [{ field: "Id", order: "desc" }] },
  });
  const { data: usersCreateBy, isLoading: isLoadingCreateBy } = useMany<IUser>({
    resource: "users",
    ids: [...new Set(tableProps?.dataSource?.map((user) => user.CreatedBy))],
  });

  // Fetch data for stats dashboard, respecting table filters
  const { data: statsData, isLoading: statsIsLoading } = useList<IProduct>({
    resource: "products",
    pagination: {
      mode: "off",
    },
    filters: filters,
  });

  const stats = useMemo(() => {
    if (!statsData?.data) {
      return {
        totalProducts: 0,
        outOfStock: 0,
        inventoryValue: 0,
      };
    }

    const products = statsData.data;
    const outOfStock = products.filter((p) => p.PhysicalQty === 0).length;
    const inventoryValue = products.reduce(
      (sum, product) => sum + product.Price * product.PhysicalQty,
      0
    );

    return {
      totalProducts: products.length,
      outOfStock,
      inventoryValue,
    };
  }, [statsData]);

  const { triggerExport, isLoading: exportLoading } = useExport<IProduct>({
    filters,
  });

  const { selectProps } = useSelect({
    resource: "users",
    optionLabel: "UserName",
    optionValue: "Id",
    defaultValue: getDefaultFilter("CreatedBy", filters, "eq"),
  });

  const [selectedRowKeys, setSelectedRowKeys] = React.useState<React.Key[]>([]);
  const { create, push } = useNavigation();

  const handleDelete = () => {
    const ids = selectedRowKeys.map((key) => key.toString());
    deleteMutate({ resource: "products", ids });
    setSelectedRowKeys([]);
  };

  const rowSelection = {
    selectedRowKeys,
    onChange: (selectedRowKeys: React.Key[]) => {
      setSelectedRowKeys(selectedRowKeys);
    },
  };

  return (
    <List
      headerButtons={
        <>
          <CanAccess resource="products" action="create">
            <Button type="primary" onClick={() => create("products")}>
              Tạo sản phẩm
            </Button>
          </CanAccess>
          <CanAccess resource="products" action="create-range">
            <Button onClick={() => push("/products/create-range")}>
              Nhập hàng loạt
            </Button>
          </CanAccess>

          <CanAccess resource="products" action="delete-range">
            <Button
              danger
              disabled={selectedRowKeys.length === 0}
              onClick={() => handleDelete()}
            >
              Xóa đã chọn {selectedRowKeys.length > 0 && `(${selectedRowKeys.length})`}
            </Button>
          </CanAccess>

          <CanAccess resource="products" action="export">
            <ExportButton onClick={triggerExport} loading={exportLoading} />
          </CanAccess>
        </>
      }
    >
      {/* Stats Dashboard Section */}
      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        <Col xs={24} sm={12} md={8}>
          <StatCard
            title="Tổng số sản phẩm"
            value={stats.totalProducts}
            loading={statsIsLoading}
            icon={<ShoppingCartOutlined />}
            color="#0052CC"
            formatter={(value) => `${Number(value || 0).toLocaleString("vi-VN")}`}
          />
        </Col>
        <Col xs={24} sm={12} md={8}>
          <StatCard
            title="Tổng giá trị kho"
            value={stats.inventoryValue}
            loading={statsIsLoading}
            icon={<DollarOutlined />}
            color="#389e0d"
            formatter={formatCurrency}
          />
        </Col>
        <Col xs={24} sm={12} md={8}>
          <StatCard
            title="Sản phẩm hết hàng"
            value={stats.outOfStock}
            loading={statsIsLoading}
            icon={<WarningOutlined />}
            color="#cf1322"
            formatter={(value) => `${Number(value || 0).toLocaleString("vi-VN")}`}
          />
        </Col>
      </Row>

      <Table
        {...tableProps}
        rowKey="Id"
        size="middle"
        sticky
        scroll={{ x: 1000 }}
        pagination={{
          ...tableProps.pagination,
          showTotal: (total) => (
            <PaginationTotal total={total} entityName="sản phẩm" />
          ),
        }}
        rowSelection={rowSelection}
      >
        <Table.Column
          dataIndex="Id"
          title="ID"
          width={80}
          sorter
          defaultSortOrder={getDefaultSortOrder("Id", sorters)}
        />
        <Table.Column
          dataIndex="Name"
          title="Sản phẩm"
          sorter
          defaultSortOrder={getDefaultSortOrder("Name", sorters)}
          defaultFilteredValue={getDefaultFilter("Name", filters)}
          filterDropdown={(props) => (
            <FilterDropdown {...props}>
              <Input placeholder="Tìm theo tên" />
            </FilterDropdown>
          )}
          render={(_, record: IProduct) => <ProductThumb record={record} />}
        />
        <Table.Column
          dataIndex="Price"
          title="Giá bán"
          align="right"
          sorter
          defaultSortOrder={getDefaultSortOrder("Price", sorters)}
          defaultFilteredValue={getDefaultFilter("Price", filters)}
          filterDropdown={(props) => (
            <FilterDropdown {...props}>
              <Input placeholder="Tìm theo giá" />
            </FilterDropdown>
          )}
          render={(value) => (
            <Typography.Text strong style={{ color: "#0052CC" }}>
              {formatCurrency(value)}
            </Typography.Text>
          )}
        />
        <Table.Column
          dataIndex="PhysicalQty"
          title="Tồn kho"
          align="center"
          sorter
          render={(value) => <StockLevelTag qty={value} />}
        />
        <Table.Column
          dataIndex="IsActive"
          title="Trạng thái"
          align="center"
          render={(value: boolean) => <ProductStatusTag isActive={value} />}
        />
        <Table.Column
          dataIndex="CreatedBy"
          title="Tạo bởi"
          render={(value) => {
            if (isLoadingCreateBy) {
              return "Loading...";
            }
            return (
              usersCreateBy?.data?.find((role) => role.Id == value)?.UserName ??
              "Not Found"
            );
          }}
          filterDropdown={(props) => (
            <FilterDropdown
              {...props}
              mapValue={(selectedKey) => String(selectedKey)}
            >
              <Select style={{ minWidth: 200 }} {...selectProps} />
            </FilterDropdown>
          )}
        />
        <Table.Column
          dataIndex="Created"
          title="Tạo lúc"
          render={(value) => <DateField format="LLL" value={value} />}
          defaultFilteredValue={getDefaultFilter("Created", filters, "between")}
          filterDropdown={(props) => (
            <FilterDropdown {...props}>
              <DatePicker.RangePicker />
            </FilterDropdown>
          )}
          sorter
          defaultSortOrder={getDefaultSortOrder("Created", sorters)}
        />
        <Table.Column
          title=""
          fixed="right"
          align="center"
          width={64}
          render={(_, record: IProduct) => <ProductActions record={record} />}
        />
      </Table>
    </List>
  );
};