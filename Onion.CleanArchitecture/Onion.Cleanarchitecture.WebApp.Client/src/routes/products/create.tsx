import { useForm, Create, getValueFromEvent } from "@refinedev/antd";
import { Form, Input, InputNumber, Switch, Row, Col, Upload, Button, message } from "antd";
import { UploadOutlined } from "@ant-design/icons";
import { IProduct } from "./types";
export const CreateProduct = () => {
  const { formProps, saveButtonProps } = useForm<IProduct>({
    redirect: "edit",
  });
  return (
    <Create saveButtonProps={saveButtonProps}>
      <Form
        {...formProps}
        layout="vertical"
        initialValues={{ IsActive: true, PhysicalQty: 0 }}
        onValuesChange={(changedValues) => {
          if (changedValues.Price) {
            formProps.form?.setFieldsValue({ Rate: changedValues.Price });
          }
        }}
      >
        <Row gutter={24}>
          <Col span={16}>
            <Form.Item
              label="Name"
              name="Name"
              rules={[
                { required: true, message: "Please input your product name!" },
                { min: 3, message: "Product name must be at least 3 characters long!" },
                { max: 50, message: "Product name must be at most 50 characters long!" },
              ]}
            >
              <Input />
            </Form.Item>
            <Form.Item
              label="Barcode"
              name="Barcode"
              rules={[
                { required: true, message: "Please input the barcode!" },
                { max: 50, message: "Barcode must be at most 50 characters long!" },
              ]}
            >
              <Input />
            </Form.Item>
            <Form.Item label="Description" name="Description">
              <Input.TextArea rows={4} />
            </Form.Item>
          </Col>
          <Col span={8}>
            {/* Trường Rate được ẩn đi nhưng giá trị sẽ được tự động cập nhật theo Price */}
            <Form.Item name="Rate" hidden>
              <InputNumber />
            </Form.Item>
            <Form.Item
              label="Price"
              name="Price"
              rules={[{ required: true, message: "Please input the price!" }]}
            >
              <InputNumber min={11} style={{ width: "100%" }} />
            </Form.Item>
            <Form.Item label="Số lượng tồn kho" name="PhysicalQty" rules={[{ required: true }]}>
              <InputNumber min={0} style={{ width: "100%" }} />
            </Form.Item>
            <Form.Item
              label="Kích hoạt"
              name="IsActive"
              valuePropName="checked"
            >
              <Switch />
            </Form.Item>
            {/* Placeholder for Image Upload */}
            <Form.Item
              label="Ảnh sản phẩm"
              name="ImageUrl"
              valuePropName="fileList"
              getValueFromEvent={getValueFromEvent}
              rules={[{ required: false }]}
            >
              <Upload
                name="file"
                action="/api/files/upload" // QUAN TRỌNG: Thay thế bằng endpoint thực tế của bạn
                listType="picture"
                maxCount={1}
                beforeUpload={(file) => {
                  const isJpgOrPng = file.type === 'image/jpeg' || file.type === 'image/png';
                  if (!isJpgOrPng) {
                    message.error('Bạn chỉ có thể tải lên file JPG/PNG!');
                  }
                  return isJpgOrPng;
                }}
              >
                <Button icon={<UploadOutlined />}>Tải ảnh lên</Button>
              </Upload>
            </Form.Item>
          </Col>
        </Row>
      </Form>
    </Create>
  );
};
