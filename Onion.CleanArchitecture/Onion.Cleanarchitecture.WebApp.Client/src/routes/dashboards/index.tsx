import React from "react";
import { Typography } from "antd";

const { Title } = Typography;

export const Dashboard: React.FC = () => {
  return (
    <div style={{ padding: "2rem", border: "2px dashed blue" }}>
      <Title level={1}>TEST DASHBOARD</Title>
      <Title level={4}>Nếu bạn thấy được nội dung này, vấn đề nằm ở logic bên trong component Dashboard cũ.</Title>
    </div>
  );
};

export { SagaMonitorDashboard } from "./saga-monitor";