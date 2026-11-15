import { Modal as AntModal, Spin } from "antd";
import { Button } from "../Button/Button";
import { Text } from "../Text/Text";
import modalStyles from "../Modal/Modal.module.less";
import styles from "./TextPreviewModal.module.less";
import { useTranslation } from "react-i18next";

interface TextPreviewNavigationButton {
  text: string;
  onClick: () => void;
  disabled?: boolean;
}

interface TextPreviewModalProps {
  open: boolean;
  title: string;
  text?: string;
  caption?: string;
  previousButton?: TextPreviewNavigationButton;
  nextButton?: TextPreviewNavigationButton;
  isLoading: boolean;
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
  previousButton,
  nextButton,
  isLoading,
  errorMessage,
  retryText,
  onRetry,
  onClose,
}: TextPreviewModalProps) {
  const { t } = useTranslation();
  const hasError = !isLoading && errorMessage !== undefined;

  const footer = isLoading ? null : hasError ? (
    <Button onClick={onRetry}>{retryText}</Button>
  ) : text !== undefined && (previousButton || nextButton) ? (
    <div className={styles.actions}>
      {previousButton && (
        <Button
          variant="secondary"
          onClick={previousButton.onClick}
          disabled={isLoading || previousButton.disabled}
        >
          {previousButton.text}
        </Button>
      )}
      {nextButton && (
        <Button
          variant="secondary"
          onClick={nextButton.onClick}
          disabled={isLoading || nextButton.disabled}
        >
          {nextButton.text}
        </Button>
      )}
    </div>
  ) : null;

  return (
    <AntModal
      open={open}
      title={isLoading ? undefined : title}
      width={760}
      className={modalStyles.modal}
      centered
      closable={!isLoading}
      maskClosable={!isLoading}
      keyboard={!isLoading}
      onCancel={isLoading ? undefined : onClose}
      footer={footer}
    >
      {isLoading ? (
        <div className={modalStyles.progress} role="status" aria-live="polite">
          <Spin size="large" />
          <Text>{t("app.pleaseWait")}</Text>
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
