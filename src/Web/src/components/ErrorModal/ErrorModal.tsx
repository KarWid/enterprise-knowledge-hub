import { Button, Modal } from "antd";
import { useTranslation } from "react-i18next";
import styles from "./ErrorModal.module.less";

interface ErrorModalProps {
  message: string | null;
  onClose: () => void;
}

export function ErrorModal({ message, onClose }: ErrorModalProps) {
  const { t } = useTranslation();

  return (
    <Modal
      open={message !== null}
      className={styles.modal}
      closable={{ "aria-label": t("errorModal.close") }}
      title={null}
      centered
      maskClosable={false}
      destroyOnHidden
      onCancel={onClose}
      footer={
        <Button type="primary" onClick={onClose}>
          {t("errorModal.next")}
        </Button>
      }
    >
      <strong className={styles.message}>{message}</strong>
    </Modal>
  );
}
