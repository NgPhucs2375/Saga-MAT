import { FC } from "react";

type PaginationTotalProps = {
  total: number;
  entityName: string;
};

export const PaginationTotal: FC<PaginationTotalProps> = ({
  total,
  entityName,
}) => {
  return (
    <span
      style={{
        marginLeft: "16px",
      }}
    >
      Tổng cộng{" "}
      <span className="ant-text secondary">{total}</span> {entityName}
    </span>
  );
};
