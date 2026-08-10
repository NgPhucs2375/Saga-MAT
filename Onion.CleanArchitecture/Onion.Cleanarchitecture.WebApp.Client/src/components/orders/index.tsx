export { ProcessSteps } from "./process-steps";

import { Card, Col, Row, Statistic, Typography } from "antd";
import {
  ShoppingCartOutlined,
  DollarCircleOutlined,
  UserOutlined,
  ArrowUpOutlined,
} from "@ant-design/icons";
import { Area, AreaConfig } from "@ant-design/plots";

const { Title, Text: TypographyText } = Typography;

export const DashboardPage = () => {
  // Dữ liệu giả lập cho biểu đồ doanh thu
  const salesData = [
    { date: "2023-01-01", value: 1200 },
    { date: "2023-01-02", value: 2200 },
    { date: "2023-01-03", value: 1800 },
    { date: "2023-01-04", value: 2500 },
    { date: "2023-01-05", value: 2100 },
    { date: "2023-01-06", value: 3300 },
    { date: "2023-01-07", value: 4500 },
  ];

  const areaConfig: AreaConfig = {
    data: salesData,
    xField: "date",
    yField: "value",
    height: 300,
    smooth: true,
    areaStyle: () => {
      return {
        fill: "l(270) 0:#ffffff 1:#52c41a", // Gradient fill
      };
    },
    line: {
      color: "#52c41a",
    },
    yAxis: {
      label: {
        formatter: (v) => `${(Number(v) / 1000).toFixed(1)}k`,
      },
    },
  };

  return (
    <div>
      <Title level={3}>Tổng quan</Title>
      <TypographyText type="secondary">Chào mừng trở lại, đây là tình hình kinh doanh của bạn hôm nay.</TypographyText>

      {/* Hàng KPI */}
      <Row gutter={[16, 16]} style={{ marginTop: 24 }}>
        <Col xs={24} sm={12} md={6}>
          <Card>
            <Statistic
              title="Doanh thu hôm nay"
              value={11289300}
              precision={0}
              valueStyle={{ color: "#3f8600" }}
              prefix={<ArrowUpOutlined />}
              suffix="VNĐ"
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Card>
            <Statistic
              title="Đơn hàng mới"
              value={32}
              prefix={<ShoppingCartOutlined />}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Card>
            <Statistic
              title="Khách hàng mới"
              value={5}
              prefix={<UserOutlined />}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Card>
            <Statistic
              title="Tỷ lệ hoàn tất"
              value={93}
              precision={1}
              suffix="%"
            />
          </Card>
        </Col>
      </Row>

      {/* Hàng Biểu đồ và Widget */}
      <Row gutter={[16, 16]} style={{ marginTop: 24 }}>
        <Col xs={24} lg={16}>
          <Card title="Doanh thu 7 ngày qua">
            <Area {...areaConfig} />
          </Card>
        </Col>
        <Col xs={24} lg={8}>
          <Card title="Sản phẩm sắp hết hàng">
            {/* Widget "Sản phẩm sắp hết hàng" sẽ được triển khai ở đây */}
            <TypographyText>1. Sản phẩm A (còn 5)</TypographyText><br/>
            <TypographyText>2. Sản phẩm B (còn 3)</TypographyText>
          </Card>
        </Col>
      </Row>
    </div>
  );
};