import { useForm, Create } from "@refinedev/antd";
import { Form, Button } from "antd";
import { IProduct } from "./types";
import { useNavigation } from "@refinedev/core";
import { ProductFormFields } from "./product-form";

export const CreateProduct = () => {
  const { list } = useNavigation();
  const {
    formProps: baseFormProps,
    saveButtonProps: baseSaveButtonProps,
    form,
  } = useForm<IProduct>({
    redirect: "edit",
  });

  // Tạo một hàm onFinish tùy chỉnh để xử lý dữ liệu trước khi gửi
  const onFinish = async (values: any) => {
    const { ImageUrl, ...rest } = values;
    let finalImageUrl = "";

    if (ImageUrl && Array.isArray(ImageUrl) && ImageUrl.length > 0) {
      const file = ImageUrl[0];
      if (file.response) {
        // Trường hợp 1: File mới được tải lên, response từ server có sẵn
        finalImageUrl =
          file.response.url ||
          (typeof file.response === "string" ? file.response : "");
      } else if (file.url) {
        // Trường hợp 2: File đã tồn tại (hữu ích khi sửa sản phẩm)
        finalImageUrl = file.url;
      }
    }

    // Gọi hàm onFinish gốc của Refine với dữ liệu đã được xử lý
    if (baseFormProps.onFinish) {
      await baseFormProps.onFinish({ ...rest, ImageUrl: finalImageUrl });
    }
  };

  const saveButtonProps = {
    ...baseSaveButtonProps,
    children: "Lưu sản phẩm",
  };

  return (
    <Create
      saveButtonProps={saveButtonProps}
      title="Tạo sản phẩm mới"
      footerButtons={({ defaultButtons }) => (
        <>
          <Button onClick={() => list("products")}>Hủy</Button>
          {defaultButtons}
        </>
      )}
    >
      <Form
        {...baseFormProps}
        onFinish={onFinish}
        layout="vertical"
        initialValues={{ IsActive: true, PhysicalQty: 0 }}
      >
        <ProductFormFields form={form} />
      </Form>
    </Create>
  );
};