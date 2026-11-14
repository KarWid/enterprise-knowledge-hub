import { Modal as AntModal, Spin } from "antd";
import { Button } from "../Button/Button";
import { Text } from "../Text/Text";
import modalStyles from "../Modal/Modal.module.less";
import styles from "./TextPreviewModal.module.less";

interface TextPreviewModalProps {
  open: boolean;
  title: string;
  text?: string;
  caption?: string;
  isLoading: boolean;
  loadingMessage: string;
  errorMessage?: string;
  retryText: string;
  onRetry: () => void;
  onClose: () => void;
}

export function TextPreviewModal({
  open,
  title,
  text,
  caption,
  isLoading,
  loadingMessage,
  errorMessage,
  retryText,
  onRetry,
  onClose,
}: TextPreviewModalProps) {
  const hasError = !isLoading && errorMessage !== undefined;
  const footer = hasError ? <Button onClick={onRetry}>{retryText}</Button> : null;

  return (
    <AntModal
      open={open}
      title={title}
      width={760}
      className={modalStyles.modal}
      centered
      onCancel={onClose}
      footer={footer}
    >
      {isLoading ? (
        <div className={modalStyles.progress} role="status" aria-live="polite">
          <Spin size="large" />
          <Text>{loadingMessage}</Text>
        </div>
      ) : hasError ? (
        <Text tone="danger">{errorMessage}</Text>
      ) : text !== undefined ? (
        <>
          <pre className={styles.content}>{text}</pre>
          {caption && <p className={styles.caption}>{caption}</p>}
        </>
      ) : null}
    </AntModal>
  );
}
