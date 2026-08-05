import {
  useTable,
  List,
  getDefaultSortOrder,
  DateField,
  FilterDropdown,
  useSelect,
  ExportButton,
} from "@refinedev/antd";
import { Table, Input, Button, DatePicker, Select, Tag, Row, Col, Card, Statistic } from "antd";
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
import { ProductActions, ProductStatusTag } from "./productcomponent";
import {
  ShoppingCartOutlined,
  DollarOutlined,
  WarningOutlined,
} from "@ant-design/icons";
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

  // const { data: usersModifiedBy, isLoading: isLoadingModifiedBy } =
  //   useMany<IUser>({
  //     resource: "users",
  //     ids: [
  //       ...new Set(tableProps?.dataSource?.map((user) => user.LastModifiedBy)),
  //     ],
  //   });

  // Fetch data for stats dashboard, respecting table filters
  const { data: statsData, isLoading: statsIsLoading } = useList<IProduct>({
    resource: "products",
    pagination: {
      mode: "off", // Fetch all records matching filters
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
    const outOfStock = products.filter(
      (p) => p.PhysicalQty === 0
    ).length;
    const inventoryValue = products.reduce(
      (sum, product) => sum + (product.Price * product.PhysicalQty),
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
            <Button onClick={() => create("products")}>Create</Button>
          </CanAccess>
          <CanAccess resource="products" action="create-range">
            <Button onClick={() => push("/products/create-range")}>
              Create range
            </Button>
          </CanAccess>

          <CanAccess resource="products" action="delete-range">
            <Button
              disabled={selectedRowKeys.length === 0}
              onClick={() => handleDelete()}
            >
              Delete range {selectedRowKeys.length}
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
          <Card>
            <Statistic
              title="Tổng số sản phẩm"
              value={stats.totalProducts}
              loading={statsIsLoading}
              prefix={<ShoppingCartOutlined />}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} md={8}>
          <Card>
            <Statistic
              title="Tổng giá trị kho"
              value={stats.inventoryValue}
              loading={statsIsLoading}
              prefix={<DollarOutlined />}
              suffix="VND"
              formatter={(value) => value.toLocaleString()}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} md={8}>
          <Card>
            <Statistic
              title="Sản phẩm hết hàng"
              value={stats.outOfStock}
              loading={statsIsLoading}
              prefix={<WarningOutlined />}
              valueStyle={{
                color: stats.outOfStock > 0 ? "#cf1322" : undefined,
              }}
            />
          </Card>
        </Col>
      </Row>

      <Table
        {...tableProps}
        rowKey="Id"
        pagination={{
          ...tableProps.pagination,
          showTotal: (total) => (
            <PaginationTotal total={total} entityName="products" />
          ),
        }}
        rowSelection={rowSelection}
      >
        <Table.Column
          dataIndex="Id"
          title="ID"
          sorter
          defaultSortOrder={getDefaultSortOrder("Id", sorters)}
        />
        <Table.Column
          dataIndex="Name"
          title="Tên sản phẩm"
          sorter
          defaultSortOrder={getDefaultSortOrder("Name", sorters)}
          defaultFilteredValue={getDefaultFilter("Name", filters)}
          filterDropdown={(props) => (
            <FilterDropdown {...props}>
              <Input placeholder="Search Name" />
            </FilterDropdown>
          )}
        />

        {/* <Table.Column
          dataIndex="Barcode"
          title="Barcode"
          sorter
          defaultSortOrder={getDefaultSortOrder("Barcode", sorters)}
          defaultFilteredValue={getDefaultFilter("Barcode", filters)}
          filterDropdown={(props) => (
            <FilterDropdown {...props}>
              <Input placeholder="Search Barcode" />
            </FilterDropdown>
          )}
        /> */}
        {/* <Table.Column
          dataIndex="Rate"
          title="Rate"
          sorter
          defaultSortOrder={getDefaultSortOrder("Rate", sorters)}
          defaultFilteredValue={getDefaultFilter("Rate", filters)}
          filterDropdown={(props) => (
            <FilterDropdown {...props}>
              <Input placeholder="Search Rate" />
            </FilterDropdown>
          )}
        /> */}
        <Table.Column
          dataIndex="Price"
          title="Giá"
          sorter
          defaultSortOrder={getDefaultSortOrder("Price", sorters)}
          defaultFilteredValue={getDefaultFilter("Price", filters)}
          filterDropdown={(props) => (
            <FilterDropdown {...props}>
              <Input placeholder="Search Price" />
            </FilterDropdown>
          )}
        />
        <Table.Column
          dataIndex="PhysicalQty"
          title="Tồn kho"
          align="right"
          sorter
          render={(value) => <Tag color={value > 0 ? "blue" : "red"}>{value ?? 0}</Tag>}
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
        {/* <Table.Column
          dataIndex="LastModifiedBy"
          title="Last Modified By"
          render={(value) => {
            if (isLoadingModifiedBy) {
              return "Loading...";
            }
            return (
              usersModifiedBy?.data?.find((role) => role.Id == value)
                ?.UserName ?? "-"
            );
          }}
        />
        <Table.Column
          dataIndex="LastModified"
          title="Last Modified"
          render={(value) => {
            if (!value) return "-";
            return <DateField format="LLL" value={value} />;
          }}
          defaultFilteredValue={getDefaultFilter(
            "LastModified",
            filters,
            "between"
          )}
          filterDropdown={(props) => (
            <FilterDropdown {...props}>
              <DatePicker.RangePicker />
            </FilterDropdown>
          )}
          sorter
          defaultSortOrder={getDefaultSortOrder("LastModified", sorters)}
        /> */}
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
          title="Hành động"
          fixed="right"
          align="center"
          width={100}
          render={(_, record: IProduct) => <ProductActions record={record} />}
        />
      </Table>
    </List>
  );
};
