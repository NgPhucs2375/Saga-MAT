import React from "react";
import { Form, Select, InputNumber, Button, Space, FormProps, Input } from "antd";
import { useSelect } from "@refinedev/antd";
import { IProduct, ICreateOrder } from "../../routes/orders/types";
// import { UseFormReturnType } from "@refinedev/antd"; // Not directly used here

interface CreateOrderFormProps {
  formProps: FormProps<ICreateOrder>;
}

export const CreateOrderForm: React.FC<CreateOrderFormProps> = ({ formProps }) => {
  const { selectProps: productSelectProps } = useSelect<IProduct>({
    resource: "products",
    optionLabel: "name",
    optionValue: "id",
  });

  return (
    <Form {...formProps} layout="vertical" initialValues={{ Items: [{}] }}>
      <Form.List name="Items">
        {(fields, { add, remove }) => {
          return (
            <div>
              {fields.map(({ key, name, ...restField }) => (
                <Space key={key} style={{ display: 'flex', marginBottom: 8, alignItems: 'end' }}>
                  <Form.Item
                    {...restField}
                    label="Product"
                    name={[name, 'productId']}
                    rules={[{ required: true, message: 'Please select a product' }]}
                    style={{ minWidth: 300 }}
                  >
                    <Select {...productSelectProps} placeholder="Select a product" />
                  </Form.Item>
                  <Form.Item
                    {...restField}
                    label="Quantity"
                    name={[name, 'quantity']}
                    rules={[{ required: true, message: 'Please input quantity' }]}
                  >
                    <InputNumber<number> placeholder="Quantity" min={1} defaultValue={1} />
                  </Form.Item>
                  <Form.Item
                    {...restField}
                    label="Price"
                    name={[name, 'price']}
                    rules={[{ required: true, message: 'Please input price' }]}
                  >
                      <InputNumber<number> placeholder="Price" min={0} style={{ minWidth: 120 }} formatter={(value) => `${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')} parser={(value) => parseFloat(value!.replace(/\$\s?|(,*)/g, '')) || 0} />
                  </Form.Item>
                  <Form.Item><Button type="dashed" danger onClick={() => remove(name)}>Remove</Button></Form.Item>
                </Space>
              ))}
              <Form.Item><Button type="dashed" onClick={() => add()} block>+ Add Order Item</Button></Form.Item>
            </div>
          );
        }}
      </Form.List>
      {/* Add other order fields here if needed, e.g., customerId, shippingAddress, note */}
      <Form.Item label="Customer ID" name="customerId">
        <Select placeholder="Select a customer" /> {/* Assuming you'll add a customer select */}
      </Form.Item>
      <Form.Item label="Shipping Address" name="shippingAddress">
        <Input placeholder="Enter shipping address" />
      </Form.Item>
      <Form.Item label="Note" name="note">
        <Input.TextArea rows={3} placeholder="Add a note" />
      </Form.Item>
    </Form>
  );
};