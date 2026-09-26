import { Button, Modal } from "antd";
import { useTranslation } from "react-i18next";
import { logout } from "../auth/authService";
import styles from "../components/ErrorModal/ErrorModal.module.less";

interface CurrentUserLoadErrorModalProps {
  onRetry: () => void;
}

export function CurrentUserLoadErrorModal({
  onRetry,
}: CurrentUserLoadErrorModalProps) {
  const { t } = useTranslation();

  function signOut() {
    void logout();
  }

  return (
    <Modal
      open
      className={styles.modal}
      closable={false}
      title={null}
      centered
      mask={false}
      maskClosable={false}
      keyboard={false}
      footer={
        <>
          <Button onClick={signOut}>{t("auth.logOut")}</Button>
          <Button type="primary" onClick={onRetry}>
            {t("errorModal.retry")}
          </Button>
        </>
      }
    >
      <strong className={styles.message}>
        {t("auth.currentUserLoadFailure")}
      </strong>
    </Modal>
  );
}
