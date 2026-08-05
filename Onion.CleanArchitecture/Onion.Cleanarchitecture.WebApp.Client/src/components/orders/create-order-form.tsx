import React, { useMemo, useState } from "react";
import { Form, Select, InputNumber, Button, FormProps, Input, Typography,  Table, Popconfirm, Card, FormListFieldData } from "antd";
import { useList } from "@refinedev/core"; // Import Card here
import { DeleteOutlined } from "@ant-design/icons";
import { IProduct, ICreateOrder } from "../../routes/orders/types";

const { Text } = Typography;

interface CreateOrderFormProps {
  formProps: FormProps<ICreateOrder>;
}

export const CreateOrderForm: React.FC<CreateOrderFormProps> = ({ formProps }) => {
  const { form } = formProps;
  const { data, isLoading } = useList<IProduct>({
    resource: "products",
    pagination: { pageSize: 100 },
  });

  const products = useMemo(() => data?.data ?? [], [data]);

  const productById = useMemo(
    () => new Map(products.map((p) => [p.ProductId, p])),
    [products]
  );

  const options = useMemo(
    () =>
      products
        .filter((p) => p.ProductId && p.ProductId !== "00000000-0000-0000-0000-000000000000")
        .map((p) => ({
          label: `${p.Name} — còn ${p.AvailableQty}`,
          value: p.ProductId,
        })),
    [products]
  );

  const [selectedProducts, setSelectedProducts] = useState<Record<number, IProduct | undefined>>({});

  const items = Form.useWatch("Items", form);

  const totalAmount = useMemo(() => {
    if (!items) return 0;
    return items.reduce((acc, item) => {
      if (!item || !item.ProductId || !item.Quantity) {
        return acc;
      }
      const product = productById.get(item.ProductId);
      if (!product) {
        return acc;
      }
      return acc + product.Price * item.Quantity;
    }, 0);
  }, [items, productById]);

  const handleProductChange = (name: number, value: string) => {
    const product = productById.get(value);
    setSelectedProducts((prev) => ({ ...prev, [name]: product }));
  };

  const formatPrice = (price: number) => price?.toLocaleString("vi-VN") ?? "";

  return (
    <Form {...formProps} layout="vertical" style={{ maxWidth: 900, margin: '0 auto' }}>
      <Form.List name="Items">
        {(fields, { add, remove }) => {
          return (
            <div>
              <Table
                dataSource={fields}
                rowKey="key"
                pagination={false}
                summary={() => (
                  <Table.Summary.Row>
                    <Table.Summary.Cell index={0} colSpan={3} align="right">
                      <Text strong style={{ fontSize: '1.2em' }}>Tổng cộng:</Text>
                    </Table.Summary.Cell>
                    <Table.Summary.Cell index={1} align="right">
                      <Typography.Title level={3} style={{ margin: 0, color: '#1677ff' }}>
                        {formatPrice(totalAmount)} đ
                      </Typography.Title>
                    </Table.Summary.Cell>
                    <Table.Summary.Cell index={2}></Table.Summary.Cell> {/* For the "Thao tác" column */}
                  </Table.Summary.Row>
                )}
              >
                <Table.Column
                  title="Sản phẩm"
                  dataIndex="ProductId"
                  key="ProductId"
                  render={(_, field: FormListFieldData) => {
                    return (
                      <Form.Item
                        name={[field.name, "ProductId"]}
                        rules={[{ required: true, message: "Chọn sản phẩm" }]}
                        style={{ margin: 0 }}
                      >
                        <Select
                          showSearch
                          optionFilterProp="label"
                          loading={isLoading}
                          placeholder="Chọn sản phẩm"
                          options={options}
                          onChange={(value) => handleProductChange(field.name, value as string)}
                          style={{ minWidth: 250 }}
                        />
                      </Form.Item>
                    );
                  }}
                />
                <Table.Column
                  title="Số lượng"
                  dataIndex="Quantity"
                  key="Quantity"
                  width={120}
                  render={(_, field: FormListFieldData) => (
                    <Form.Item
                      name={[field.name, "Quantity"]}
                      rules={[{ required: true, message: "Nhập số lượng" }]}
                      style={{ margin: 0 }}
                    >
                      <InputNumber<number> placeholder="SL" min={1} style={{ width: "100%" }} />
                    </Form.Item>
                  )}
                />
                <Table.Column
                  title="Đơn giá"
                  key="UnitPrice"
                  width={150}
                  align="right"
                  render={(_, field: FormListFieldData) => {
                    const selected = selectedProducts[field.name];
                    return (
                      <div style={{ padding: "5px 8px", background: "#f5f5f5", borderRadius: 4, textAlign: "right" }}>
                        {selected ? (
                          <>
                            <Text strong>{formatPrice(selected.Price)} đ</Text>
                            <br />
                            <Text type={selected.AvailableQty > 0 ? "success" : "danger"}>
                              Còn {selected.AvailableQty}
                            </Text>
                          </>
                        ) : (
                          <Text type="secondary">Chọn SP</Text>
                        )}
                      </div>
                    );
                  }}
                />
                <Table.Column
                  title="Thành tiền"
                  key="Subtotal"
                  width={150}
                  align="right"
                  render={(_, field: FormListFieldData) => {
                    const selected = selectedProducts[field.name];
                    const currentItem = items?.[field.name];
                    const quantity = currentItem?.Quantity;
                    const subtotal = selected && quantity ? selected.Price * quantity : 0;
                    return (
                      <div style={{ padding: "5px 8px", background: "#f5f5f5", borderRadius: 4, textAlign: "right" }}>
                        <Text strong style={{ color: "#1677ff" }}>
                          {formatPrice(subtotal)} đ
                        </Text>
                      </div>
                    );
                  }}
                />
                <Table.Column
                  title="Thao tác"
                  key="action"
                  width={80}
                  align="center"
                  render={(_, field: FormListFieldData) => (
                    <Popconfirm
                      title="Bạn có chắc muốn xóa sản phẩm này?"
                      onConfirm={() => remove(field.name)}
                      okText="Có"
                      cancelText="Không"
                    >
                      <Button type="text" danger icon={<DeleteOutlined />} />
                    </Popconfirm>
                  )}
                />
              </Table>
              <Button type="dashed" onClick={() => add()} style={{ marginTop: 16 }}>
                + Thêm sản phẩm
              </Button>
            </div>
          );
        }}
      </Form.List>

      <Card title="Thông tin giao vận & Ghi chú" style={{ marginTop: 24 }}>
        <Form.Item label="Ghi chú" name="Note">
          <Input.TextArea rows={3} placeholder="Nhập mô tả / ghi chú" />
        </Form.Item>
        <Form.Item label="Địa chỉ giao hàng" name="ShippingAddress" rules={[{ required: true, message: "Nhập địa chỉ" }]}>
          <Input placeholder="Nhập địa chỉ giao hàng" />
        </Form.Item>
      </Card>
    </Form>
  );
};
