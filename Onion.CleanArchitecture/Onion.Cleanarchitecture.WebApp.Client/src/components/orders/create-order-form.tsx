import React, { useMemo, useState } from "react";
import { Form, Select, InputNumber, Button, Space, FormProps, Input, Typography } from "antd";
import { useList } from "@refinedev/core";
import { IProduct, ICreateOrder } from "../../routes/orders/types";

const { Text } = Typography;

interface CreateOrderFormProps {
  formProps: FormProps<ICreateOrder>;
}

export const CreateOrderForm: React.FC<CreateOrderFormProps> = ({ formProps }) => {
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
      products.map((p) => ({
        label: `${p.Name} — còn ${p.AvailableQty}`,
        value: p.ProductId,
      })),
    [products]
  );

  const [selectedProducts, setSelectedProducts] = useState<Record<number, IProduct>>({});

  const handleProductChange = (name: number, value: string) => {
    const product = productById.get(value);
    setSelectedProducts((prev) => ({ ...prev, [name]: product ?? undefined }));
  };

  const formatPrice = (price: number) => price?.toLocaleString("vi-VN") ?? "";

  return (
    <Form {...formProps} layout="vertical" initialValues={{ Items: [{}] }}>
      <Form.List name="Items">
        {(fields, { add, remove }) => {
          return (
            <div>
              {fields.map(({ key, name }) => {
                const selected = selectedProducts[name];
                return (
                  <Space key={key} style={{ display: "flex", marginBottom: 8, alignItems: "start" }} align="start">
                    <Form.Item
                      label="Sản phẩm"
                      name={[name, "ProductId"]}
                      rules={[{ required: true, message: "Chọn sản phẩm" }]}
                      style={{ minWidth: 300 }}
                    >
                      <Select
                        showSearch
                        optionFilterProp="label"
                        loading={isLoading}
                        placeholder="Chọn sản phẩm"
                        options={options}
                        onChange={(value) => handleProductChange(name, value as string)}
                      />
                    </Form.Item>
                    <Form.Item
                      label="Số lượng"
                      name={[name, "Quantity"]}
                      rules={[{ required: true, message: "Nhập số lượng" }]}
                    >
                      <InputNumber<number> placeholder="Số lượng" min={1} style={{ minWidth: 100 }} />
                    </Form.Item>
                    {selected ? (
                      <Form.Item label="Giá / Khả dụng">
                        <div style={{ paddingTop: 4, lineHeight: 1.4 }}>
                          <div>
                            <Text strong>{formatPrice(selected.Price)} đ</Text>
                          </div>
                          <Text type={selected.AvailableQty > 0 ? "success" : "danger"}>
                            Còn {selected.AvailableQty}
                          </Text>
                        </div>
                      </Form.Item>
                    ) : (
                      <Form.Item label="Giá / Khả dụng">
                        <Text type="secondary">Chọn sản phẩm để xem giá & tồn kho</Text>
                      </Form.Item>
                    )}
                    <Form.Item label=" ">
                      <Button type="dashed" danger onClick={() => remove(name)}>Xóa</Button>
                    </Form.Item>
                  </Space>
                );
              })}
              <Form.Item>
                <Button type="dashed" onClick={() => add()} block>+ Thêm sản phẩm</Button>
              </Form.Item>
            </div>
          );
        }}
      </Form.List>

      <Form.Item label="Ghi chú" name="Note">
        <Input.TextArea rows={3} placeholder="Nhập mô tả / ghi chú" />
      </Form.Item>
      <Form.Item label="Địa chỉ giao hàng" name="ShippingAddress" rules={[{ required: true, message: "Nhập địa chỉ" }]}>
        <Input placeholder="Nhập địa chỉ giao hàng" />
      </Form.Item>
    </Form>
  );
};
