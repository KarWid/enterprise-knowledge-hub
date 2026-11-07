import type { HTMLAttributes, ReactNode } from "react";
import { ErrorIcon, InfoIcon, WarningIcon } from "../../icons";
import styles from "./InlineMessage.module.less";

export type InlineMessageVariant = "info" | "warning" | "error";

interface InlineMessageProps extends HTMLAttributes<HTMLDivElement> {
  variant?: InlineMessageVariant;
  children: ReactNode;
}

const icons = {
  info: InfoIcon,
  warning: WarningIcon,
  error: ErrorIcon,
};

export function InlineMessage({
  variant = "info",
  children,
  className,
  role,
  ...rest
}: InlineMessageProps) {
  const Icon = icons[variant];
  const classes = [styles.message, styles[variant], className].filter(Boolean).join(" ");

  return (
    <div
      className={classes}
      role={role ?? (variant === "error" ? "alert" : "status")}
      aria-atomic="true"
      {...rest}
    >
      <Icon className={styles.icon} />
      <div className={styles.content}>{children}</div>
    </div>
  );
}
