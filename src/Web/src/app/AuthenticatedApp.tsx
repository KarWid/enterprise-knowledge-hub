import { useTranslation } from 'react-i18next';
import { UserInfo } from '../components/UserInfo/UserInfo';
import styles from './AuthenticatedApp.module.less';
import { useGetMeQuery } from '../services/api/generated/api';
import { UserOnboardingStatusType } from '../services/api/enums';

export function AuthenticatedApp() {
  const { t } = useTranslation();
  const { data } = useGetMeQuery();

  if (data?.onboardingStatus === UserOnboardingStatusType.CreateOrganization){
    return <div>{t('onboarding.createOrganization')}</div>;
  }

  return (
    <div className={styles.shell}>
      <header className={styles.topbar}>
        <span className={styles.topbarLogo}>{t('app.title')}</span>
        <UserInfo />
      </header>

      <nav className={styles.sidebar}>
        <div className={styles.navSection}>
          <span className={styles.navLabel}>{t('nav.main')}</span>
          <a href="#chats" className={styles.navItem}>
            <span className={styles.navIcon}>💬</span>
            {t('nav.chats')}
          </a>
          <a href="#documents" className={styles.navItem}>
            <span className={styles.navIcon}>📄</span>
            {t('nav.documents')}
          </a>
        </div>
      </nav>

      <main className={styles.main}>
        <h1>{t('authenticated.welcome')}</h1>
      </main>
    </div>
  );
}
