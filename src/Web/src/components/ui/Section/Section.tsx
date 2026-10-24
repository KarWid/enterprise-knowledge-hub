import type { HTMLAttributes, ReactNode } from "react";
import styles from "./Section.module.less";

interface SectionProps extends HTMLAttributes<HTMLElement> {
  children: ReactNode;
}

export function Section({ children, className, ...rest }: SectionProps) {
  return (
    <section
      className={`${styles.section}${className ? ` ${className}` : ""}`}
      {...rest}
    >
      {children}
    </section>
  );
}
