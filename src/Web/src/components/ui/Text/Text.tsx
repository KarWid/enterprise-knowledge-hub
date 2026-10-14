import type { HTMLAttributes } from "react";
import styles from "./Text.module.less";

export type TextHeight = "small" | "medium" | "large";
export type TextWeight = "standard" | "bold";
export type TextTone =
  | "primary"
  | "secondary"
  | "muted"
  | "accent"
  | "success"
  | "warning"
  | "danger"
  | "inherit";
export type TextAlignment = "start" | "center" | "end";

export interface TextProps extends HTMLAttributes<HTMLParagraphElement> {
  height?: TextHeight;
  weight?: TextWeight;
  tone?: TextTone;
  alignment?: TextAlignment;
  truncate?: boolean;
}

export function Text({
  height = "medium",
  weight = "standard",
  tone = "secondary",
  alignment,
  truncate = false,
  className,
  ...rest
}: TextProps) {
  const classes = [
    styles.text,
    styles[height],
    styles[weight],
    styles[tone],
    alignment ? styles[alignment] : undefined,
    truncate ? styles.truncate : undefined,
    className,
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <p className={classes} {...rest} />
  );
}

