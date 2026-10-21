import { Modal as AntModal, Spin } from "antd";
import type { MouseEventHandler, ReactNode } from "react";
import { Button } from "../Button/Button";
import { Text } from "../Text/Text";
import styles from "./Modal.module.less";

export interface ModalProps {
  open: boolean;
  title?: ReactNode;
  text?: ReactNode;
  isProgress?: boolean;
  buttonText?: ReactNode;
  onClick?: MouseEventHandler<HTMLButtonElement>;
  onClose?: () => void;
  dismissible?: boolean;
  className?: string;
}

export function Modal({
  open,
  title,
  text,
  isProgress = false,
  buttonText,
  onClick,
  onClose,
  dismissible = true,
  className,
}: ModalProps) {
  const showButton = buttonText !== undefined && onClick !== undefined;
  const canDismiss = dismissible && !isProgress && onClose !== undefined;

  return (
    <AntModal
      open={open}
      title={title}
      className={`${styles.modal}${className ? ` ${className}` : ""}`}
      centered
      closable={canDismiss}
      maskClosable={canDismiss}
      keyboard={canDismiss}
      onCancel={canDismiss ? onClose : undefined}
      footer={showButton ? <Button onClick={onClick}>{buttonText}</Button> : null}
    >
      <div
        className={`${styles.content}${isProgress ? ` ${styles.progress}` : ""}`}
        role={isProgress ? "status" : undefined}
        aria-live={isProgress ? "polite" : undefined}
      >
        {isProgress && <Spin size="large" />}
        {text !== undefined && <Text>{text}</Text>}
      </div>
    </AntModal>
  );
}
