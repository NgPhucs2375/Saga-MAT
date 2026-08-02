import { useForm, Create } from "@refinedev/antd";
import { CreateOrderForm } from "@components/orders/create-order-form";
import { ICreateOrder } from "./types";

export const CreateOrder = () => {
  const { formProps, saveButtonProps } = useForm<ICreateOrder>({
    resource: "orders",
    redirect: false,
  });

  return (
    <Create saveButtonProps={saveButtonProps} title="Khởi tạo Đơn hàng (Trigger Saga)">
      <CreateOrderForm formProps={formProps} saveButtonProps={saveButtonProps} />
    </Create>
  );
};
