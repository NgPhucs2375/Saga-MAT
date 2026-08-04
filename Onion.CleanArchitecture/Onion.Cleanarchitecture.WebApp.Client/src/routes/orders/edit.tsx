import { useForm, Edit } from "@refinedev/antd"; // Import UseFormReturnType
import { CreateOrderForm } from "@components/orders/create-order-form";
import { ICreateOrder, IOrderDetail } from "./types"; // IOrderItem is not directly needed here
import { EditOutlined } from "@ant-design/icons"; // Removed SaveOutlined as it's not used directly
import { Space, Typography, FormProps } from "antd";
import { HttpError } from "@refinedev/core";

const { Text } = Typography;

export const EditOrder = () => {
  const { formProps: baseFormProps, saveButtonProps, queryResult } = useForm<IOrderDetail, HttpError, ICreateOrder>({
    resource: "orders",
    action: "edit",
    redirect: "show", // Chuyển về trang chi tiết sau khi sửa thành công
    // Refine will automatically populate initialValues from the queryResult.data
    // No need to manually map OrderItems to Items here, as ICreateOrder already uses 'Items'
  });

  // Use baseFormProps directly, as ICreateOrder is now aligned with backend payload
  const formProps: FormProps<ICreateOrder> = baseFormProps;
  const orderCode = queryResult?.data?.data?.OrderCode;

  return (
    <Edit
      saveButtonProps={saveButtonProps}
      isLoading={queryResult?.isFetching}
      title={
        <Space>
          <EditOutlined />
          <Text>Chỉnh sửa Đơn hàng #{orderCode}</Text>
        </Space>
      }
    >
      <CreateOrderForm formProps={formProps} />
    </Edit>
  );
};