import { Button, Modal } from "antd";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import styles from "./ErrorModal.module.less";

interface ErrorModalProps {
  error: unknown;
  fallbackMessage: string;
  onContinue: () => void;
}

export function ErrorModal({
  error,
  fallbackMessage,
  onContinue,
}: ErrorModalProps) {
  const { t } = useTranslation();
  const [dismissedError, setDismissedError] = useState<unknown>(null);
  const message =
    error === dismissedError ? null : getApiErrorMessage(error, fallbackMessage);

  function dismiss() {
    setDismissedError(error);
  }

  function continueRequest() {
    dismiss();
    onContinue();
  }

  return (
    <Modal
      open={message !== null}
      className={styles.modal}
      closable={{ "aria-label": t("errorModal.close") }}
      title={null}
      centered
      maskClosable={false}
      destroyOnHidden
      onCancel={dismiss}
      footer={
        <Button type="primary" onClick={continueRequest}>
          {t("errorModal.next")}
        </Button>
      }
    >
      <strong className={styles.message}>{message}</strong>
    </Modal>
  );
}

function getApiErrorMessage(error: unknown, fallbackMessage: string): string | null {
  if (error === undefined) {
    return null;
  }

  if (typeof error !== "object" || error === null || !("data" in error)) {
    return fallbackMessage;
  }

  const { data } = error;
  if (typeof data !== "object" || data === null || !("message" in data)) {
    return fallbackMessage;
  }

  const { message } = data;
  return typeof message === "string" && message.trim().length > 0
    ? message
    : fallbackMessage;
}
