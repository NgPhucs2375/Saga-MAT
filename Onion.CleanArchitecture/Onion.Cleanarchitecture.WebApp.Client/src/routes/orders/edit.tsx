import { useForm, Edit } from "@refinedev/antd"; // Import UseFormReturnType
import { CreateOrderForm } from "@components/orders/create-order-form";
import { ICreateOrder, IOrderDetail } from "./types";
import { EditOutlined } from "@ant-design/icons";
import { Space, Typography, FormProps } from "antd";
import { HttpError } from "@refinedev/core";

const { Text: TypographyText } = Typography;

export const EditOrder = () => {
  const { formProps: baseFormProps, saveButtonProps, queryResult } = useForm<IOrderDetail, HttpError, ICreateOrder>({
    resource: "orders",
    action: "edit",
    redirect: "show",
    // Map the data from the query to the form's expected shape
    queryOptions: {
      select: (data) => {
        return {
          ...data,
          data: {
            ...data.data,
            // Map OrderItems from IOrderDetail to Items for ICreateOrder
            Items: data.data.OrderItems?.map(p => ({ ProductId: p.ProductId, Quantity: p.Quantity })) || [],
          },
        };
      },
    },
  });

  const formProps: FormProps<ICreateOrder> = baseFormProps;
  const orderCode = queryResult?.data?.data?.OrderCode;

  return (
    <Edit
      saveButtonProps={saveButtonProps}
      isLoading={queryResult?.isFetching}
      title={
        <Space>
          <EditOutlined />
          <TypographyText>Chỉnh sửa Đơn hàng #{orderCode}</TypographyText>
        </Space>
      }
    >
      <CreateOrderForm formProps={formProps} />
    </Edit>
  );
};