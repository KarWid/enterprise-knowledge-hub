import type { ButtonHTMLAttributes } from "react";
import styles from "./Button.module.less";

type ButtonVariant = "primary" | "secondary";

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
}

export function Button({
  variant = "primary",
  className,
  type = "button",
  ...rest
}: ButtonProps) {
  return (
    <button
      type={type}
      className={`${styles.button} ${styles[variant]}${className ? ` ${className}` : ""}`}
      {...rest}
    />
  );
}
