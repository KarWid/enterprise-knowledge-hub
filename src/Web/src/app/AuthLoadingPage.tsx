import { useTranslation } from 'react-i18next';
import styles from './AuthLoadingPage.module.css';

interface Props {
  message: string;
}

export function AuthLoadingPage({ message }: Props) {
  const { t } = useTranslation();

  return (
    <div className={styles.page}>
      <span className={styles.logo}>{t('app.title')}</span>
      <div className={styles.spinner} role="status" aria-label={message} />
      <p className={styles.message}>{message}</p>
    </div>
  );
}
