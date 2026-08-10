import React, { useMemo } from "react";
import { Steps, Tag, Typography, Space } from "antd";
import { DateField } from "@refinedev/antd";
import {
  SendOutlined,
  SafetyCertificateOutlined,
  CheckCircleOutlined,
  ThunderboltOutlined,
  FlagOutlined,
  CloseCircleOutlined,
} from "@ant-design/icons";
import { IOrderHistory, OrderStatus } from "@routes/orders/types";

const { Text: TypographyText } = Typography;

enum HistoryStatus {
  Success = 1,
  Failed = 2,
}

type StepState = "wait" | "process" | "finish" | "error";
type Terminal = "completed" | "cancelled" | null;

interface ProcessHistory extends IOrderHistory {
  EventType?: string;
}

interface Props {
  histories?: IOrderHistory[];
  orderStatus?: OrderStatus;
}

const STEP_META = [
  { title: "Đã gửi", subTitle: "Submitted" },
  { title: "Xác thực", subTitle: "Validating" },
  { title: "Duyệt đơn", subTitle: "Accepting" },
  { title: "Hoàn tất", subTitle: "Completing" },
  { title: "Hoàn thành", subTitle: "Completed" },
];

function isCompensation(h: ProcessHistory): boolean {
  const event = h.EventType ?? "";
  return (
    h.ConsumerName === "ReleaseInventoryConsumer" ||
    h.ConsumerName === "CancelOrderConsumer" ||
    (h.ConsumerName === "OrderAcceptConsumer" &&
      (event === "CancelOrderCommand" || event === "Compensate")) ||
    (h.ConsumerName === "OrderCompleteConsumer" &&
      event === "ReleaseInventoryCommand")
  );
}

function stepIndexOf(h: ProcessHistory): number | null {
  if (isCompensation(h)) return 4;
  switch (h.ConsumerName) {
    case "OrderSubmitConsumer":
      return 1;
    case "OrderAcceptConsumer":
      return 2;
    case "OrderCompleteConsumer":
      return 3;
    default:
      return null;
  }
}

function computeStates(
  histories: ProcessHistory[],
  orderStatus?: OrderStatus
): { states: StepState[]; currentIndex: number; terminal: Terminal } {
  const states: StepState[] = ["finish", "wait", "wait", "wait", "wait"];
  let terminal: Terminal = null;

  if (orderStatus === OrderStatus.Completed) {
    return {
      states: ["finish", "finish", "finish", "finish", "finish"],
      currentIndex: 4,
      terminal: "completed",
    };
  }

  if (orderStatus === OrderStatus.Rejected) {
    return {
      states: ["finish", "finish", "finish", "finish", "error"],
      currentIndex: 4,
      terminal: "cancelled",
    };
  }

  for (const h of histories) {
    const idx = stepIndexOf(h);
    if (idx === null) continue;
    const ok = h.Status === HistoryStatus.Success;
    states[idx] = ok ? "finish" : "error";
    if (!ok) terminal = "cancelled";
  }

  let currentIndex = -1;
  for (let i = 1; i <= 4; i++) {
    if (states[i] === "error") {
      currentIndex = i;
      terminal = "cancelled";
      break;
    }
  }
  if (currentIndex === -1) {
    for (let i = 1; i <= 4; i++) {
      if (states[i] === "wait") {
        states[i] = "process";
        currentIndex = i;
        break;
      }
    }
    if (currentIndex === -1) {
      currentIndex = 4;
      states[4] = "finish";
      terminal = "completed";
    }
  }

  return { states, currentIndex, terminal };
}

function groupByStep(histories: ProcessHistory[]): ProcessHistory[][] {
  const groups: ProcessHistory[][] = [[], [], [], [], []];
  for (const h of histories) {
    const idx = stepIndexOf(h);
    if (idx !== null) groups[idx].push(h);
  }
  return groups;
}

function getIcon(index: number, terminal: Terminal) {
  switch (index) {
    case 0:
      return <SendOutlined />;
    case 1:
      return <SafetyCertificateOutlined />;
    case 2:
      return <CheckCircleOutlined />;
    case 3:
      return <ThunderboltOutlined />;
    case 4:
      return terminal === "cancelled" ? <CloseCircleOutlined /> : <FlagOutlined />;
    default:
      return undefined;
  }
}

export const ProcessSteps: React.FC<Props> = ({ histories = [], orderStatus }) => {
  const sorted = useMemo(
    () =>
      [...histories].sort(
        (a, b) =>
          new Date(a.CreatedAt).getTime() - new Date(b.CreatedAt).getTime()
      ),
    [histories]
  );

  const { states, currentIndex, terminal } = useMemo(
    () => computeStates(sorted, orderStatus),
    [sorted, orderStatus]
  );

  const groups = useMemo(() => groupByStep(sorted), [sorted]);

  const meta = [...STEP_META];
  if (terminal === "cancelled") {
    meta[4] = { title: "Từ chối", subTitle: "Rejected / Cancelled" };
  }

  return (
    <Steps
      direction="vertical"
      size="small"
      current={currentIndex}
      status={terminal === "cancelled" ? "error" : "process"}
      items={meta.map((step, index) => ({
        title: step.title,
        status: states[index],
        icon: getIcon(index, terminal),
        description: (
          <StepDetails
            entries={groups[index]}
            subTitle={step.subTitle}
          />
        ),
      }))}
    />
  );
};

const StepDetails: React.FC<{ entries: ProcessHistory[]; subTitle: string }> = ({
  entries,
  subTitle,
}) => {
  if (entries.length === 0) {
    return (
      <div style={{ marginTop: 2 }}>
        <TypographyText type="secondary" style={{ fontSize: 12 }}>
          {subTitle} · chưa có hoạt động
        </TypographyText>
      </div>
    );
  }

  return (
    <Space direction="vertical" size={2} style={{ marginTop: 4, width: "100%" }}>
      {entries.map((h, idx) => {
        const isSuccess = h.Status === HistoryStatus.Success;
        return (
          <div
            key={`${h.id ?? h.ConsumerName}-${h.CreatedAt ?? ""}-${idx}`}
            style={{ marginBottom: 6 }}
          >
            <Space size={4} wrap>
              <Tag
                color={isSuccess ? "success" : "error"}
                style={{ marginInlineEnd: 0 }}
              >
                {isSuccess ? "THÀNH CÔNG" : "THẤT BẠI"}
              </Tag>
              <TypographyText strong style={{ fontSize: 12 }}>
                {h.ConsumerName}
              </TypographyText>
            </Space>
            {h.Message && (
              <div style={{ marginTop: 2 }}>
                <TypographyText type="secondary" style={{ fontSize: 12 }}>
                  {h.Message}
                </TypographyText>
              </div>
            )}
            {h.CreatedAt && (
              <div style={{ marginTop: 2 }}>
                <DateField
                  style={{ fontSize: 11 }}
                  value={h.CreatedAt}
                  format="DD/MM/YYYY HH:mm:ss"
                />
              </div>
            )}
          </div>
        );
      })}
    </Space>
  );
};
