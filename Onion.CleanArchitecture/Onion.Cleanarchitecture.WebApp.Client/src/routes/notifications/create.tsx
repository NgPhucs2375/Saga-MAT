import React from "react";
import { Card, Typography, Empty } from "antd";

const { Title, Text } = Typography;

export const CreateNoti: React.FC = () => {
  return (
    <Card>
      <Title level={4}>Tạo thông báo</Title>
      <Empty
        image={Empty.PRESENTED_IMAGE_SIMPLE}
        description={<Text type="secondary">Tính năng đang phát triển.</Text>}
      />
    </Card>
  );
};
