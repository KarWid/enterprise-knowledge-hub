import { useTranslation } from 'react-i18next';
import { LogoutButton } from '../../components/LogoutButton';
import styles from './AcceptInvitationPage.module.less';

export function AcceptInvitationPage() {
  const { t } = useTranslation();

  return (
    <div className={styles.page}>
      <LogoutButton className={styles.logout} />
      <div className={styles.card}>
        <span className={styles.icon} aria-hidden="true">⚠️</span>
        <h1 className={styles.heading}>{t('onboarding.acceptInvitationErrorHeading')}</h1>
        <p className={styles.message}>{t('onboarding.acceptInvitationError')}</p>
      </div>
    </div>
  );
}
