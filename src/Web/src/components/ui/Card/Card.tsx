import type { HTMLAttributes, ReactNode } from "react";
import styles from "./Card.module.less";

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode;
}

export function Card({ children, className, ...rest }: CardProps) {
  return (
    <div
      className={`${styles.card}${className ? ` ${className}` : ""}`}
      {...rest}
    >
      {children}
    </div>
  );
}
