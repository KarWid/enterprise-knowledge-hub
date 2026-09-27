import type { HTMLAttributes, ReactNode } from "react";
import styles from "./Heading.module.less";

type HeadingLevel = "h1" | "h2" | "h3";

interface HeadingProps extends Omit<HTMLAttributes<HTMLDivElement>, "title"> {
  level: HeadingLevel;
  title: ReactNode;
  subtitle?: ReactNode;
}

export function Heading({
  level,
  title,
  subtitle,
  className,
  ...rest
}: HeadingProps) {
  const TitleElement = level;

  return (
    <div
      className={`${styles.heading}${className ? ` ${className}` : ""}`}
      {...rest}
    >
      <TitleElement className={`${styles.title} ${styles[level]}`}>
        {title}
      </TitleElement>
      {subtitle && <p className={styles.subtitle}>{subtitle}</p>}
    </div>
  );
}
