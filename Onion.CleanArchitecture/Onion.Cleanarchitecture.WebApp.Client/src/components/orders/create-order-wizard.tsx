import React, { useState, useMemo, useEffect } from "react";
import {
  Form,
  Input,
  InputNumber,
  Button,
  Space,
  Card,
  Row,
  Col,
  Typography,
  Empty,
  App,
  Spin,
  Steps,
  Alert,
  Checkbox,
  Divider,
  theme,
  type FormProps,
} from "antd";
import {
  SearchOutlined,
  MinusOutlined,
  DeleteOutlined,
  ShoppingCartOutlined,
  DollarCircleOutlined,
  CheckOutlined,
  RightOutlined,
  LeftOutlined,
  ShoppingOutlined,
  FormOutlined,
  GiftOutlined,
} from "@ant-design/icons";
import { useList } from "@refinedev/core";
import type { SaveButtonProps } from "@refinedev/antd";
import { ICreateOrder, ICreateOrderItem, IProduct } from "@routes/orders/types";
import { ProductCard } from "./product-card";

const { Text: TypographyText } = Typography;

interface CreateOrderWizardProps {
  formProps: FormProps<ICreateOrder>;
  saveButtonProps: SaveButtonProps;
}

const DISCOUNT_CODES: Record<
  string,
  { type: "percent" | "fixed"; value: number }
> = {
  GIAM10: { type: "percent", value: 10 },
  FREESHIP: { type: "fixed", value: 20000 },
};

const STEP_ITEMS = [
  { title: "Chọn sản phẩm", icon: <ShoppingOutlined /> },
  { title: "Xem lại đơn", icon: <FormOutlined /> },
  { title: "Giao hàng & Hoàn tất", icon: <CheckOutlined /> },
];

export const CreateOrderWizard: React.FC<CreateOrderWizardProps> = ({
  formProps,
  saveButtonProps,
}) => {
  const { message } = App.useApp();
  const { token } = theme.useToken();

  const [currentStep, setCurrentStep] = useState(0);
  const [selectedProducts, setSelectedProducts] = useState<
    Record<string, { product: IProduct; quantity: number }>
  >({});
  const [searchTerm, setSearchTerm] = useState<string>("");
  const [showLowStockOnly, setShowLowStockOnly] = useState(false);
  const [discountCode, setDiscountCode] = useState("");
  const [appliedDiscount, setAppliedDiscount] = useState(0);

  const { data: productsData, isLoading: productsLoading } = useList<IProduct>({
    resource: "products",
    filters: [{ field: "IsActive", operator: "eq", value: true }],
    pagination: { mode: "off" },
  });

  const allProducts = useMemo(() => productsData?.data || [], [productsData?.data]);

  const filteredProducts = useMemo(() => {
    let result = allProducts;
    if (searchTerm) {
      const lower = searchTerm.toLowerCase();
      result = result.filter(
        (p) =>
          p.Name.toLowerCase().includes(lower) ||
          p.Code.toLowerCase().includes(lower)
      );
    }
    if (showLowStockOnly) {
      result = result.filter((p) => p.AvailableQty <= 10 && p.AvailableQty > 0);
    }
    return result;
  }, [allProducts, searchTerm, showLowStockOnly]);

  const cartItems = useMemo(() => Object.values(selectedProducts), [selectedProducts]);

  const totalItems = useMemo(
    () => cartItems.reduce((sum, { quantity }) => sum + quantity, 0),
    [cartItems]
  );

  const subtotal = useMemo(
    () =>
      cartItems.reduce(
        (sum, { product, quantity }) => sum + product.Price * quantity,
        0
      ),
    [cartItems]
  );

  const totalAmount = useMemo(
    () => Math.max(0, subtotal - appliedDiscount),
    [subtotal, appliedDiscount]
  );

  useEffect(() => {
    const items: ICreateOrderItem[] = cartItems.map(
      ({ product, quantity }) => ({
        ProductId: product.ProductId,
        Quantity: quantity,
      })
    );
    formProps.form?.setFieldsValue({ Items: items });
  }, [cartItems, formProps.form]);

  const handleAddProduct = (product: IProduct) => {
    setSelectedProducts((prev) => {
      const currentQty = prev[product.ProductId]?.quantity || 0;
      if (currentQty >= product.AvailableQty) {
        message.warning(`${product.Name} đã hết hàng khả dụng!`);
        return prev;
      }
      return {
        ...prev,
        [product.ProductId]: { product, quantity: currentQty + 1 },
      };
    });
  };

  const handleUpdateQuantity = (productId: string, newQuantity: number) => {
    setSelectedProducts((prev) => {
      const item = prev[productId];
      if (!item) return prev;
      if (newQuantity <= 0) {
        const next = { ...prev };
        delete next[productId];
        return next;
      }
      if (newQuantity > item.product.AvailableQty) {
        message.warning(
          `${item.product.Name} chỉ còn ${item.product.AvailableQty} khả dụng.`
        );
        return {
          ...prev,
          [productId]: { ...item, quantity: item.product.AvailableQty },
        };
      }
      return { ...prev, [productId]: { ...item, quantity: newQuantity } };
    });
  };

  const handleRemoveProduct = (productId: string) => {
    setSelectedProducts((prev) => {
      const next = { ...prev };
      delete next[productId];
      return next;
    });
  };

  const handleApplyDiscount = () => {
    const code = discountCode.trim().toUpperCase();
    if (!code) {
      message.warning("Vui lòng nhập mã giảm giá.");
      return;
    }
    const rule = DISCOUNT_CODES[code];
    if (!rule) {
      message.error("Mã giảm giá không hợp lệ.");
      setAppliedDiscount(0);
      return;
    }
    const discount =
      rule.type === "percent"
        ? Math.round((subtotal * rule.value) / 100)
        : Math.min(rule.value, subtotal);
    setAppliedDiscount(discount);
    message.success(`Đã áp dụng mã ${code}: giảm ${discount.toLocaleString()} VND`);
  };

  const hasOutOfStockItems = cartItems.some(
    ({ product, quantity }) => quantity > product.AvailableQty
  );

  return (
    <Form {...formProps} layout="vertical" initialValues={{ ...formProps.initialValues, Items: [] }}>
      <Card className="order-wizard-steps" style={{ marginBottom: 16 }}>
        <Steps
          current={currentStep}
          items={STEP_ITEMS.map((step, index) => ({
            title: step.title,
            icon: index < currentStep ? <CheckOutlined /> : step.icon,
            status:
              index < currentStep
                ? "finish"
                : index === currentStep
                ? "process"
                : "wait",
          }))}
        />
      </Card>

      {hasOutOfStockItems && (
        <Alert
          message="Có sản phẩm vượt quá tồn kho"
          description="Vui lòng điều chỉnh số lượng trước khi tạo đơn"
          type="warning"
          showIcon
          style={{ marginBottom: 16 }}
        />
      )}

      {/* BƯỚC 1 - CHỌN SẢN PHẨM */}
      {currentStep === 0 && (
        <Card
          title={
            <Space>
              <ShoppingOutlined />
              <TypographyText strong>Chọn sản phẩm</TypographyText>
            </Space>
          }
        >
          <Space direction="vertical" style={{ width: "100%" }} size="middle">
            <Input
              size="large"
              prefix={<SearchOutlined style={{ color: token.colorTextPlaceholder }} />}
              placeholder="Tìm kiếm sản phẩm theo tên hoặc mã..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              allowClear
            />
            <Checkbox
              checked={showLowStockOnly}
              onChange={(e) => setShowLowStockOnly(e.target.checked)}
            >
              Chỉ hiện sản phẩm sắp hết hàng (≤10)
            </Checkbox>

            {productsLoading ? (
              <div style={{ textAlign: "center", padding: "60px 20px" }}>
                <Spin tip="Đang tải sản phẩm..." size="large" />
              </div>
            ) : filteredProducts.length === 0 ? (
              <Empty
                description={
                  searchTerm || showLowStockOnly
                    ? "Không tìm thấy sản phẩm phù hợp."
                    : "Chưa có sản phẩm nào."
                }
                image={Empty.PRESENTED_IMAGE_SIMPLE}
                imageStyle={{ height: 80 }}
                style={{ padding: "40px 0" }}
              />
            ) : (
              <Row gutter={[16, 16]}>
                {filteredProducts.map((product) => {
                  const selectedQty =
                    selectedProducts[product.ProductId]?.quantity || 0;
                  return (
                    <Col xs={24} sm={12} md={8} lg={6} key={product.ProductId}>
                      <ProductCard
                        product={product}
                        selectedQuantity={selectedQty}
                        onAdd={handleAddProduct}
                      />
                    </Col>
                  );
                })}
              </Row>
            )}
          </Space>

          <Divider style={{ marginTop: 16 }} />
          <div className="order-wizard-footer">
            <div>
              <TypographyText strong>
                <ShoppingCartOutlined style={{ marginRight: 6 }} />
                Đã chọn {totalItems} sản phẩm
              </TypographyText>
              <div>
                <TypographyText type="secondary">Tạm tính: </TypographyText>
                <TypographyText strong style={{ color: token.colorPrimary }}>
                  {subtotal.toLocaleString()} VND
                </TypographyText>
              </div>
            </div>
            <Button
              type="primary"
              size="large"
              icon={<RightOutlined />}
              disabled={totalItems === 0}
              onClick={() => setCurrentStep(1)}
            >
              Xác nhận danh sách sản phẩm
            </Button>
          </div>
        </Card>
      )}

      {/* BƯỚC 2 - XEM LẠI ĐƠN */}
      {currentStep === 1 && (
        <Card
          title={
            <Space>
              <FormOutlined />
              <TypographyText strong>Xem lại danh sách món đã chọn</TypographyText>
            </Space>
          }
        >
          {cartItems.length === 0 ? (
            <Empty
              description="Giỏ hàng đang trống."
              image={Empty.PRESENTED_IMAGE_SIMPLE}
              style={{ padding: "40px 0" }}
            />
          ) : (
            <Space direction="vertical" style={{ width: "100%" }} size="middle">
              {cartItems.map(({ product, quantity }) => {
                const lineTotal = product.Price * quantity;
                return (
                  <Card key={product.ProductId} size="small" bordered>
                    <Row align="middle" gutter={[16, 12]}>
                      <Col xs={24} md={10} style={{ minWidth: 0 }}>
                        <TypographyText strong>{product.Name}</TypographyText>
                        <div>
                          <TypographyText type="secondary" style={{ fontSize: 12 }}>
                            Mã: {product.Code} · {product.Price.toLocaleString()} VND
                          </TypographyText>
                        </div>
                      </Col>
                      <Col xs={12} md={6}>
                        <Space>
                          <Button
                            size="small"
                            icon={<MinusOutlined />}
                            onClick={() =>
                              handleUpdateQuantity(product.ProductId, quantity - 1)
                            }
                            disabled={quantity <= 1}
                          />
                          <InputNumber
                            min={1}
                            max={product.AvailableQty}
                            value={quantity}
                            onChange={(v) =>
                              handleUpdateQuantity(product.ProductId, v || 0)
                            }
                            style={{ width: 64, textAlign: "center" }}
                            controls={false}
                          />
                          <Button
                            size="small"
                            icon={<RightOutlined />}
                            onClick={() =>
                              handleUpdateQuantity(product.ProductId, quantity + 1)
                            }
                            disabled={quantity >= product.AvailableQty}
                          />
                        </Space>
                      </Col>
                      <Col xs={12} md={5} style={{ textAlign: "right" }}>
                        <TypographyText strong style={{ fontSize: 15, color: token.colorPrimary }}>
                          {lineTotal.toLocaleString()} VND
                        </TypographyText>
                      </Col>
                      <Col xs={24} md={3} style={{ textAlign: "right" }}>
                        <Button
                          type="text"
                          danger
                          icon={<DeleteOutlined />}
                          onClick={() => handleRemoveProduct(product.ProductId)}
                        >
                          Xóa
                        </Button>
                      </Col>
                    </Row>
                  </Card>
                );
              })}

              <Divider />

              <Row gutter={[16, 16]} align="middle">
                <Col xs={24} md={12}>
                  <Space.Compact style={{ width: "100%" }}>
                    <Input
                      prefix={<GiftOutlined style={{ color: token.colorTextPlaceholder }} />}
                      placeholder="Nhập mã giảm giá (GIAM10 / FREESHIP)"
                      value={discountCode}
                      onChange={(e) => setDiscountCode(e.target.value)}
                      onPressEnter={handleApplyDiscount}
                      allowClear
                    />
                    <Button type="default" onClick={handleApplyDiscount}>
                      Áp dụng
                    </Button>
                  </Space.Compact>
                </Col>
                <Col xs={24} md={12}>
                  <Space direction="vertical" style={{ width: "100%" }} size={4}>
                    <Row justify="space-between">
                      <Col>
                        <TypographyText type="secondary">Tạm tính</TypographyText>
                      </Col>
                      <Col>
                        <TypographyText>{subtotal.toLocaleString()} VND</TypographyText>
                      </Col>
                    </Row>
                    {appliedDiscount > 0 && (
                      <Row justify="space-between">
                        <Col>
                          <TypographyText type="secondary">Giảm giá</TypographyText>
                        </Col>
                        <Col>
                          <TypographyText style={{ color: token.colorSuccess }}>
                            -{appliedDiscount.toLocaleString()} VND
                          </TypographyText>
                        </Col>
                      </Row>
                    )}
                    <Divider style={{ margin: "8px 0" }} />
                    <Row justify="space-between" align="middle">
                      <Col>
                        <TypographyText strong style={{ fontSize: 16 }}>
                          Tổng cộng
                        </TypographyText>
                      </Col>
                      <Col>
                        <TypographyText strong style={{ fontSize: 22, color: token.colorPrimary }}>
                          <DollarCircleOutlined style={{ marginRight: 6 }} />
                          {totalAmount.toLocaleString()} VND
                        </TypographyText>
                      </Col>
                    </Row>
                  </Space>
                </Col>
              </Row>
            </Space>
          )}

          <Divider style={{ marginTop: 16 }} />
          <div className="order-wizard-footer">
            <Button icon={<LeftOutlined />} onClick={() => setCurrentStep(0)}>
              Quay lại
            </Button>
            <Button
              type="primary"
              size="large"
              icon={<RightOutlined />}
              disabled={totalItems === 0}
              onClick={() => setCurrentStep(2)}
            >
              Tiếp tục
            </Button>
          </div>
        </Card>
      )}

      {/* BƯỚC 3 - GIAO HÀNG & HOÀN TẤT */}
      {currentStep === 2 && (
        <Card
          title={
            <Space>
              <CheckOutlined />
              <TypographyText strong>Thông tin giao hàng</TypographyText>
            </Space>
          }
        >
          <Form.Item
            label="Địa chỉ giao hàng"
            name="ShippingAddress"
            rules={[{ required: true, message: "Vui lòng nhập địa chỉ giao hàng!" }]}
          >
            <Input.TextArea rows={3} placeholder="Nhập địa chỉ giao hàng..." />
          </Form.Item>
          <Form.Item label="Ghi chú" name="Note">
            <Input.TextArea rows={2} placeholder="Ghi chú thêm (nếu có)..." />
          </Form.Item>

          <Divider />
          <div className="order-wizard-footer">
            <Button icon={<LeftOutlined />} onClick={() => setCurrentStep(1)}>
              Quay lại
            </Button>
            <Button type="primary" size="large" icon={<CheckOutlined />} {...saveButtonProps}>
              Tạo đơn hàng
            </Button>
          </div>
        </Card>
      )}

      <Form.Item name="Items" hidden>
        <Input />
      </Form.Item>
    </Form>
  );
};
