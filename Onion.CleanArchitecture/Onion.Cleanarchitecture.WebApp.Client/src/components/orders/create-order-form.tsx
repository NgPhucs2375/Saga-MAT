import React, { useState, useEffect } from "react";
import { Form, Input, InputNumber, Select, Button, Space, Card, Divider, Typography } from "antd";
import { MinusCircleOutlined, PlusOutlined } from "@ant-design/icons";
import { useList } from "@refinedev/core";
import { IProduct } from "@routes/products/types";
import "./order-form.css";

const { Text, Title } = Typography;

interface CreateOrderFormProps {
  formProps: any;
  saveButtonProps: any;
}

export const CreateOrderForm: React.FC<CreateOrderFormProps> = ({
  formProps,
  saveButtonProps,
}) => {
  const { data: productsData, isLoading: isProductsLoading } = useList<IProduct>({
    resource: "products",
    pagination: { pageSize: 100 },
  });

  const products = productsData?.data ?? [];

  // Để tính tổng tiền hiển thị trên UI realtime
  const [items, setItems] = useState<any[]>([]);

  // Lắng nghe giá trị form thay đổi để tính lại tổng tiền
  const onValuesChange = (changedValues: any, allValues: any) => {
    if (allValues.Items) {
      setItems(allValues.Items);
    }
  };

  const calculateTotal = () => {
    let total = 0;
    items.forEach((item) => {
      if (item && item.ProductId && item.Quantity) {
        const prod = products.find((p) => p.ProductId === item.ProductId);
        if (prod) {
          total += prod.Price * item.Quantity;
        }
      }
    });
    return total;
  };

  return (
    <Form
      {...formProps}
      layout="vertical"
      onValuesChange={onValuesChange}
      className="order-form-container"
    >
      <Form.Item
        label="Địa chỉ giao hàng (Shipping Address)"
        name="ShippingAddress"
        rules={[{ required: true, message: "Vui lòng nhập địa chỉ giao hàng!" }]}
      >
        <Input placeholder="Nhập địa chỉ nhận hàng" />
      </Form.Item>

      <Form.Item label="Ghi chú (Note)" name="Note">
        <Input.TextArea rows={2} placeholder="Ghi chú thêm về đơn hàng" />
      </Form.Item>

      <Divider orientation="left">Danh sách sản phẩm</Divider>

      <Form.List
        name="Items"
        rules={[
          {
            validator: async (_, names) => {
              if (!names || names.length < 1) {
                return Promise.reject(new Error("Phải chọn ít nhất 1 sản phẩm!"));
              }
            },
          },
        ]}
      >
        {(fields, { add, remove }, { errors }) => (
          <Space direction="vertical" style={{ width: "100%" }}>
            {fields.map(({ key, name, ...restField }) => {
              // Lấy ProductId đã chọn trong hàng này để hiển thị thông tin tồn kho & đơn giá
              const currentItem = items[name];
              const selectedProduct = currentItem
                ? products.find((p) => p.ProductId === currentItem.ProductId)
                : null;

              return (
                <Card
                  key={key}
                  size="small"
                  className="order-item-row"
                  actions={[
                    <MinusCircleOutlined
                      key="delete"
                      onClick={() => remove(name)}
                      style={{ color: "#ff4d4f" }}
                    />,
                  ]}
                >
                  <Space align="baseline" wrap>
                    <Form.Item
                      {...restField}
                      name={[name, "ProductId"]}
                      rules={[{ required: true, message: "Vui lòng chọn sản phẩm!" }]}
                      style={{ minWidth: 250, marginBottom: 0 }}
                    >
                      <Select
                        placeholder="Chọn sản phẩm"
                        loading={isProductsLoading}
                        showSearch
                        optionFilterProp="children"
                      >
                        {products.map((prod) => (
                          <Select.Option key={prod.ProductId} value={prod.ProductId}>
                            {prod.Name} (Kho: {prod.SLTKho})
                          </Select.Option>
                        ))}
                      </Select>
                    </Form.Item>

                    <Form.Item
                      {...restField}
                      name={[name, "Quantity"]}
                      rules={[
                        { required: true, message: "Vui lòng nhập số lượng!" },
                        {
                          validator: async (_, value) => {
                            if (value && value < 1) {
                              return Promise.reject(new Error("Số lượng phải lớn hơn 0!"));
                            }
                            if (selectedProduct && value && value > selectedProduct.SLTKho) {
                              return Promise.reject(
                                new Error(`Chỉ còn ${selectedProduct.SLTKho} trong kho!`)
                              );
                            }
                          },
                        },
                      ]}
                      style={{ marginBottom: 0 }}
                    >
                      <InputNumber placeholder="Số lượng" min={1} />
                    </Form.Item>

                    {selectedProduct && (
                      <Space direction="vertical" size={0} style={{ marginLeft: 8 }}>
                        <Text type="secondary">
                          Đơn giá: {selectedProduct.Price.toLocaleString()} VND
                        </Text>
                        <Text type="secondary">
                          Thành tiền:{" "}
                          {(selectedProduct.Price * (currentItem?.Quantity ?? 0)).toLocaleString()}{" "}
                          VND
                        </Text>
                      </Space>
                    )}
                  </Space>
                </Card>
              );
            })}

            <Form.Item style={{ marginBottom: 0 }}>
              <Button
                type="dashed"
                onClick={() => add()}
                block
                icon={<PlusOutlined />}
              >
                Thêm sản phẩm
              </Button>
              <Form.ErrorList errors={errors} />
            </Form.Item>
          </Space>
        )}
      </Form.List>

      {items.length > 0 && (
        <Card className="order-total-section">
          <Space direction="vertical" style={{ width: "100%" }}>
            <Title level={4} style={{ margin: 0 }}>
              Tổng tiền đơn hàng:
            </Title>
            <Title level={2} type="danger" style={{ margin: 0 }}>
              {calculateTotal().toLocaleString()} VND
            </Title>
          </Space>
        </Card>
      )}

      <Space style={{ marginTop: 24, width: "100%" }}>
        <Button
          type="primary"
          htmlType="submit"
          {...saveButtonProps}
          className="order-submit-button"
        >
          Gửi đơn hàng (Trigger Saga)
        </Button>
      </Space>
    </Form>
  );
};
