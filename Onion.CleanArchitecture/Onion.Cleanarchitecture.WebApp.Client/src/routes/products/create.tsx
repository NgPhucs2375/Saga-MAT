import { useForm, Create, getValueFromEvent } from "@refinedev/antd";
import { Form, Input, InputNumber, Switch, Row, Col, Upload,  Card } from "antd";
import {  InboxOutlined } from "@ant-design/icons";
import { IProduct } from "./types";

const { Dragger } = Upload;

export const CreateProduct = () => {
  const { formProps, saveButtonProps } = useForm<IProduct>({
    redirect: "edit",
  });

  const draggerProps = {
    name: "file",
    action: "/api/files/upload", 
    listType: "picture-card" as const,
    maxCount: 1,
    accept: "image/png, image/jpeg",
  };

  return (
    <Create saveButtonProps={saveButtonProps} title="Tạo sản phẩm mới">
      <Form
        {...formProps}
        layout="vertical"
        initialValues={{ IsActive: true, PhysicalQty: 0 }}
        onValuesChange={(changedValues  ) => {
          if (changedValues.Price) {
            formProps.form?.setFieldsValue({ Rate: changedValues.Price });
          }
          // Xử lý khi upload file thành công
          if (changedValues.ImageUrl) {
            const file = changedValues.ImageUrl[0];
            if (file && file.status === 'done') {
              const serverResponse = file.response;
              if (serverResponse && serverResponse.url) {
                // Cập nhật giá trị thực tế để gửi đi
                formProps.form?.setFieldsValue({ ImageUrl: serverResponse.url });
              }
            }
          }
        }}
      >
        <Row gutter={24}>
          {/* Main Info Column */}
          <Col xs={24} lg={16}>
            <Card title="Thông tin sản phẩm">
              <Form.Item
                label="Tên sản phẩm"
                name="Name"
                rules={[
                  { required: true, message: "Vui lòng nhập tên sản phẩm!" },
                  { min: 3, message: "Tên sản phẩm phải có ít nhất 3 ký tự!" },
                  { max: 50, message: "Tên sản phẩm không được vượt quá 50 ký tự!" },
                ]}
              >
                <Input />
              </Form.Item>
              <Form.Item
                label="Mã vạch "
                name="Barcode"
                rules={[
                  { required: true, message: "Vui lòng nhập mã vạch!" },
                  { max: 50, message: "Mã vạch không được vượt quá 50 ký tự!" },
                ]}
              >
                <Input />
              </Form.Item>
              <Form.Item label="Mô tả" name="Description">
                <Input.TextArea rows={4} />
              </Form.Item>
            </Card>
          </Col>
          {/* Side Column */}
          <Col xs={24} lg={8}>
            <Card title="Giá & Tồn kho" style={{ marginBottom: 24 }}>
              <Form.Item name="Rate" hidden><InputNumber /></Form.Item>
              <Form.Item
                label="Giá bán"
                name="Price"
                rules={[{ required: true, message: "Vui lòng nhập giá bán!" }]}
              >
                <InputNumber<number>
                  min={11}
                  style={{ width: "100%" }}
                  formatter={(value) => `${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')}
                  parser={(value) => Number(value!.replace(/\$\s?|(,*)/g, ''))}
                  addonAfter="VNĐ"
                />
              </Form.Item>
              <Form.Item label="Số lượng tồn kho" name="PhysicalQty" rules={[{ required: true, message: "Vui lòng nhập số lượng tồn kho!" }]}>
                <InputNumber<number> min={0} style={{ width: "100%" }} />
              </Form.Item>
            </Card>
            <Card title="Trạng thái & Hình ảnh">
              <Form.Item label="Trạng thái" name="IsActive" valuePropName="checked">
                <Switch checkedChildren="Đang bán" unCheckedChildren="Ngừng bán" />
              </Form.Item>
              <Form.Item label="Hình ảnh sản phẩm">
                <Form.Item name="ImageUrl" valuePropName="fileList" getValueFromEvent={getValueFromEvent} noStyle>
                  <Dragger {...draggerProps}>
                    <p className="ant-upload-drag-icon"><InboxOutlined /></p>
                    <p className="ant-upload-text">Nhấn hoặc kéo file vào đây để tải lên</p>
                    <p className="ant-upload-hint">Chỉ hỗ trợ ảnh PNG, JPG.</p>
                  </Dragger>
                </Form.Item>
              </Form.Item>
            </Card>
          </Col>
        </Row>
      </Form>
    </Create>
  );
};
