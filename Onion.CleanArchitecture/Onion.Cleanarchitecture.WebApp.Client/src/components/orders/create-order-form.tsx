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
  Badge,
  Alert,
  Select,
  Checkbox,
  Table,
  theme,
  type FormProps,
} from "antd";
import { PlusOutlined, MinusOutlined, DeleteOutlined, ShoppingCartOutlined, DollarCircleOutlined } from "@ant-design/icons";
import { useList } from "@refinedev/core";
import { ICreateOrder, ICreateOrderItem, IProduct } from "@routes/orders/types";

const { Text: TypographyText } = Typography;

interface CreateOrderFormProps {
  formProps: FormProps<ICreateOrder>;
}

export const CreateOrderForm: React.FC<CreateOrderFormProps> = ({ formProps }) => {
  const { message } = App.useApp();
  const { token } = theme.useToken();
  const [selectedProducts, setSelectedProducts] = useState<Record<string, { product: IProduct; quantity: number }>>({});
  const [searchTerm, setSearchTerm] = useState<string>("");
  const [showLowStockOnly, setShowLowStockOnly] = useState(false);

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
      result = result.filter(p => p.Name.toLowerCase().includes(lower) || p.Code.toLowerCase().includes(lower));
    }
    if (showLowStockOnly) result = result.filter(p => p.AvailableQty <= 10 && p.AvailableQty > 0);
    return result;
  }, [allProducts, searchTerm, showLowStockOnly]);

  const productOptions = useMemo(() =>
    filteredProducts.map(p => ({
      label: `${p.Name} (${p.Code}) - ${p.Price.toLocaleString()} VND - Tồn: ${p.SLTKho} - Khả dụng: ${Math.max(0, p.AvailableQty - (selectedProducts[p.ProductId]?.quantity || 0))}`,
      value: p.ProductId,
    })), [filteredProducts, selectedProducts]);

  const totalAmount = useMemo(() =>
    Object.values(selectedProducts).reduce((sum, { product, quantity }) => sum + product.Price * quantity, 0)
  , [selectedProducts]);

  const totalItems = useMemo(() =>
    Object.values(selectedProducts).reduce((sum, { quantity }) => sum + quantity, 0)
  , [selectedProducts]);

  useEffect(() => {
    const items: ICreateOrderItem[] = Object.values(selectedProducts).map(({ product, quantity }) => ({
      ProductId: product.ProductId,
      Quantity: quantity,
    }));
    formProps.form?.setFieldsValue({ Items: items });
  }, [selectedProducts, formProps.form]);

  useEffect(() => {
    if (formProps.initialValues?.Items && allProducts.length > 0) {
      const initialSelected: Record<string, { product: IProduct; quantity: number }> = {};
      (formProps.initialValues.Items as ICreateOrderItem[]).forEach(item => {
        const product = allProducts.find(p => p.ProductId === item.ProductId);
        if (product) initialSelected[product.ProductId] = { product, quantity: item.Quantity };
      });
      setSelectedProducts(initialSelected);
    }
  }, [formProps.initialValues, allProducts]);

  const handleAddProduct = (product: IProduct) => {
    setSelectedProducts(prev => {
      const currentQty = prev[product.ProductId]?.quantity || 0;
      if (currentQty >= product.SLTKho) {
        message.warning(`${product.Name} đã hết hàng khả dụng!`);
        return prev;
      }
      return { ...prev, [product.ProductId]: { product, quantity: currentQty + 1 } };
    });
  };

  const handleUpdateQuantity = (productId: string, newQuantity: number) => {
    setSelectedProducts(prev => {
      const item = prev[productId];
      if (!item) return prev;
      if (newQuantity <= 0) {
        const next = { ...prev }; delete next[productId]; return next;
      }
      if (newQuantity > item.product.AvailableQty) {
        message.warning(`${item.product.Name} chỉ còn ${item.product.AvailableQty} khả dụng.`);
        return { ...prev, [productId]: { ...item, quantity: item.product.AvailableQty } };
      }
      return { ...prev, [productId]: { ...item, quantity: newQuantity } };
    });
  };

  const handleRemoveProduct = (productId: string) => {
    setSelectedProducts(prev => { const next = { ...prev }; delete next[productId]; return next; });
  };

  const hasOutOfStockItems = Object.values(selectedProducts).some(({ product, quantity }) => quantity > product.AvailableQty);

  const getAvailableColor = (available: number) =>
    available < 0 ? token.colorError : available <= 5 ? token.colorWarning : token.colorSuccess;

  return (
    <Form {...formProps} layout="vertical" initialValues={{ ...formProps.initialValues, Items: [] }}>
      {hasOutOfStockItems && (
        <Alert message="Có sản phẩm vượt quá tồn kho" description="Vui lòng điều chỉnh số lượng trước khi lưu"
          type="warning" showIcon style={{ marginBottom: 16 }} />
      )}

      <Row gutter={[24, 24]}>
        {/* LEFT: Shipping + Product Selection */}
        <Col xs={24} lg={13}>
          <Card title="Thông tin giao hàng" style={{ marginBottom: 16 }}>
            <Form.Item label="Địa chỉ giao hàng" name="ShippingAddress"
              rules={[{ required: true, message: "Vui lòng nhập địa chỉ giao hàng!" }]}>
              <Input.TextArea rows={3} placeholder="Nhập địa chỉ giao hàng..." />
            </Form.Item>
            <Form.Item label="Ghi chú" name="Note">
              <Input.TextArea rows={2} placeholder="Ghi chú thêm (nếu có)..." />
            </Form.Item>
          </Card>

          <Card title="Chọn sản phẩm" size="small">
            <div style={{ marginBottom: 16 }}>
              <Space size="middle" wrap>
                <Select
                  placeholder="Tìm kiếm và chọn sản phẩm..."
                  showSearch
                  filterOption={(input, option) => option.label?.toLowerCase().includes(input.toLowerCase())}
                  style={{ width: "100%", maxWidth: 460 }} allowClear maxTagCount={1} maxTagPlaceholder="Đã chọn"
                  onSearch={setSearchTerm}
                  notFoundContent={filteredProducts.length === 0 ? "Không tìm thấy sản phẩm" : undefined}
                  dropdownRender={menu => (
                    <>
                      <div style={{ padding: 8, borderBottom: "1px solid #f0f0f0", display: "flex", gap: 8, alignItems: "center" }}>
                        <Checkbox checked={showLowStockOnly} onChange={e => setShowLowStockOnly(e.target.checked)}>
                          Chỉ hiện sắp hết hàng (≤10)
                        </Checkbox>
                        <Badge count={filteredProducts.length} color="blue" />
                      </div>
                      {menu}
                    </>
                  )}
                >
                  {productOptions.map(opt => <Select.Option key={opt.value} value={opt.value}>{opt.label}</Select.Option>)}
                </Select>
              </Space>
            </div>

            <div style={{ maxHeight: 300, overflowY: "auto", border: "1px solid #f0f0f0", borderRadius: 6 }}>
              {productsLoading ? (
                <div style={{ textAlign: "center", padding: "40px 20px" }}><Spin tip="Đang tải sản phẩm..." size="large" /></div>
              ) : filteredProducts.length === 0 ? (
                <Empty description={searchTerm || showLowStockOnly ? "Không tìm thấy sản phẩm phù hợp." : "Chưa có sản phẩm nào."}
                  image={Empty.PRESENTED_IMAGE_SIMPLE} imageStyle={{ height: 80 }} />
              ) : (
                <Table dataSource={filteredProducts} rowKey="ProductId" pagination={false} size="small"
                  columns={[
                    { title: "Sản phẩm", dataIndex: "Name", key: "Name", width: 200,
                      render: (_, r: IProduct) => (
                        <div><TypographyText strong>{r.Name}</TypographyText><br/>
                          <TypographyText type="secondary" style={{ fontSize: 12 }}>Mã: {r.Code}</TypographyText></div>
                      )},
                    { title: "Đơn giá", dataIndex: "Price", key: "Price", width: 120, align: "right",
                      render: (v: number) => <TypographyText>{v.toLocaleString()} VND</TypographyText> },
                    {
                      title: "Khả dụng", dataIndex: "AvailableQty", key: "AvailableQty", width: 110, align: "center",
                      render: (value: number, record: IProduct) => {
                        const selectedQty = selectedProducts[record.ProductId]?.quantity || 0;
                        const available = value - selectedQty;
                        return (
                          <div style={{ display: "flex", flexDirection: "column", gap: 3, alignItems: "center" }}>
                            <div style={{ fontWeight: 600, fontSize: 14, color: "rgba(0,0,0,0.88)" }}>Tồn: {value}</div>
                            {selectedProducts[record.ProductId]?.quantity && (
                              <div style={{ fontSize: 12, color: token.colorPrimary }}>Đã chọn: {selectedProducts[record.ProductId]?.quantity}</div>
                            )}
                            <div style={{ fontWeight: 600, fontSize: 13, color: available < 0 ? token.colorError : available <= 5 ? token.colorWarning : token.colorSuccess }}>
                              Khả dụng: {Math.max(0, available)}
                            </div>
                          </div>
                        );
                      }
                    },
                    {
                      title: "Thao tác", key: "action", width: 130, fixed: "right",
                      render: (_, record: IProduct) => {
                        const selectedQty = selectedProducts[record.ProductId]?.quantity || 0;
                        const available = record.AvailableQty - selectedQty;
                        if (selectedQty > 0) {
                          return (
                            <Space size={4}>
                              <Button size="small" icon={<MinusOutlined />} onClick={() => handleUpdateQuantity(record.ProductId, selectedQty - 1)} disabled={selectedQty <= 1} />
                              <InputNumber min={1} max={record.AvailableQty} value={selectedQty}
                                onChange={v => handleUpdateQuantity(record.ProductId, v || 0)} style={{ width: 60 }} controls={false} />
                              <Button size="small" icon={<PlusOutlined />} onClick={() => handleUpdateQuantity(record.ProductId, selectedQty + 1)} disabled={selectedQty >= record.AvailableQty} />
                            </Space>
                          );
                        }
                        return (
                          <Button type={available <= 0 ? "default" : "primary"} size="small" icon={<PlusOutlined />}
                            onClick={() => handleAddProduct(record)} disabled={available <= 0}>
                            {available <= 0 ? "Hết hàng" : "Thêm"}
                          </Button>
                        );
                      }
                    }
                  ]}
                />
              )}
            </div>
          </Card>
        </Col>

        {/* RIGHT: Shopping Cart */}
        <Col xs={24} lg={11}>
          <Card title={<Space><ShoppingCartOutlined /><TypographyText>Giỏ hàng ({totalItems} món)</TypographyText></Space>} size="small" style={{ minHeight: 650 }}>
            {Object.keys(selectedProducts).length === 0 ? (
              <div style={{ textAlign: "center", padding: "60px 20px" }}>
                <Empty description="Chưa có sản phẩm nào trong giỏ hàng. Chọn sản phẩm bên trái để thêm."
                  image={Empty.PRESENTED_IMAGE_SIMPLE} imageStyle={{ height: 100 }} />
              </div>
            ) : (
              <div style={{ maxHeight: 500, overflowY: "auto" }}>
                <Space direction="vertical" style={{ width: "100%" }} size="middle">
                  {Object.values(selectedProducts).map(({ product, quantity }) => {
                    const availableInCart = product.AvailableQty - quantity;
                    const isOverStock = availableInCart < 0;
                    const isLowStock = availableInCart <= 5 && availableInCart >= 0;
                    const availableColor = getAvailableColor(availableInCart);
                    return (
                      <Card key={product.ProductId} size="small" bordered
                            style={{ background: isOverStock ? token.colorErrorBg : isLowStock ? token.colorWarningBg : undefined,
                              border: isOverStock ? `1px solid ${token.colorErrorBorder}` : isLowStock ? `1px solid ${token.colorWarningBorder}` : undefined, transition: "all 0.2s" }}>
                        <Row align="middle" gutter={[16, 12]}>
                          <Col xs={24} md={10} style={{ minWidth: 0, display: "flex", flexDirection: "column", gap: 8 }}>
                            <div style={{ display: "flex", flexWrap: "wrap", gap: 8, alignItems: "center" }}>
                              <TypographyText strong style={{ fontSize: 14, flex: 1, minWidth: 0 }} ellipsis>{product.Name}</TypographyText>
                              <TypographyText type="secondary" style={{ fontSize: 12 }}>Mã: {product.Code}</TypographyText>
                            </div>
                            <div style={{ display: "flex", gap: 16, alignItems: "center", flexWrap: "wrap", fontSize: 13 }}>
                              <span style={{ color: token.colorPrimary, fontWeight: 500 }}>{product.Price.toLocaleString()} VND</span>
                              <span style={{ color: "rgba(0,0,0,0.88)" }}>Khả dụng: <strong>{product.AvailableQty}</strong></span>
                              <span style={{ color: token.colorPrimary }}>Đã chọn: <strong>{quantity}</strong></span>
                              <span style={{ color: availableColor, fontWeight: 600 }}>Khả dụng: <strong>{Math.max(0, availableInCart)}</strong></span>
                            </div>
                          </Col>
                          <Col xs={24} md={8} style={{ display: "flex", gap: 12, alignItems: "center", justifyContent: "flex-end", flexWrap: "wrap" }}>
                            <Input.Group compact style={{ width: 150 }}>
                              <Button icon={<MinusOutlined />} onClick={() => handleUpdateQuantity(product.ProductId, quantity - 1)} disabled={quantity <= 1} />
                              <InputNumber min={1} max={product.AvailableQty} value={quantity}
                                onChange={v => handleUpdateQuantity(product.ProductId, v || 0)} style={{ textAlign: "center", width: 50 }} controls={false} />
                              <Button icon={<PlusOutlined />} onClick={() => handleUpdateQuantity(product.ProductId, quantity + 1)} disabled={quantity >= product.AvailableQty} />
                            </Input.Group>
                            <TypographyText strong style={{ fontSize: 16, color: isOverStock ? token.colorError : undefined, whiteSpace: "nowrap" }}>
                              {(quantity * product.Price).toLocaleString()} VND
                            </TypographyText>
                            <Button type="text" danger icon={<DeleteOutlined />} onClick={() => handleRemoveProduct(product.ProductId)} size="small">Xóa</Button>
                          </Col>
                        </Row>
                      </Card>
                    );
                  })}
                </Space>
              </div>
            )}

            {Object.keys(selectedProducts).length > 0 && (
              <div style={{ marginTop: 16, paddingTop: 16, borderTop: "1px solid #f0f0f0" }}>
                <Row justify="space-between" align="middle">
                  <Col>
                    <Space direction="vertical" size={2}>
                      <TypographyText strong style={{ fontSize: 16 }}>Tổng cộng: {totalItems} sản phẩm</TypographyText>
                      <TypographyText type="secondary" style={{ fontSize: 13 }}>Đã trừ tồn kho khả dụng khi đặt hàng</TypographyText>
                    </Space>
                  </Col>
                  <Col>
                    <div style={{ textAlign: "right" }}>
                      <TypographyText strong style={{ fontSize: 24, color: token.colorPrimary }}>
                        <DollarCircleOutlined style={{ marginRight: 6 }} />
                        {totalAmount.toLocaleString()} VND
                      </TypographyText>
                    </div>
                  </Col>
                </Row>
              </div>
            )}
          </Card>
        </Col>
      </Row>

      <Form.Item name="Items" hidden><Input /></Form.Item>
    </Form>
  );
};