import { useForm, Create } from "@refinedev/antd";
import { CreateOrderForm } from "@components/orders/create-order-form";
import { ICreateOrder } from "./types";
import { BaseRecord, HttpError } from "@refinedev/core";

export const CreateOrder = () => {
  const { formProps, saveButtonProps } = useForm<BaseRecord, HttpError, ICreateOrder>({
    resource: "orders",
    redirect: "list",
  });

  return (
    <Create saveButtonProps={saveButtonProps} title="Tạo Đơn hàng ">
      <CreateOrderForm formProps={formProps} />
    </Create>
  );
};
