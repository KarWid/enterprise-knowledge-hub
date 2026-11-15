import { Tooltip as AntTooltip } from "antd";
import type { ReactElement } from "react";
import styles from "./Tooltip.module.less";

interface TooltipProps {
  content: string;
  children: ReactElement;
}

export function Tooltip({ content, children }: TooltipProps) {
  return (
    <AntTooltip
      title={<span className={styles.content}>{content}</span>}
      trigger={["hover", "focus"]}
    >
      {children}
    </AntTooltip>
  );
}
