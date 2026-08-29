import { useTranslation } from 'react-i18next';
import { useGetMeQuery } from '../../services/api/baseApi';
import { logout } from '../../auth/authService';
import styles from './UserInfo.module.less';

export function UserInfo() {
  const { t } = useTranslation();
  const { data } = useGetMeQuery();

  const initials = data?.name
    ? data.name.split(' ').map((n: string) => n[0]).slice(0, 2).join('').toUpperCase()
    : '?';

  return (
    <div className={styles.wrapper}>
      <div className={styles.avatar} title={data?.name}>
        {initials}
      </div>
      <div className={styles.info}>
        {data?.name && <span className={styles.name}>{data.name}</span>}
        {data?.email && <span className={styles.email}>{data.email}</span>}
      </div>
      <button
        className={styles.logoutButton}
        onClick={logout}
        title={t('auth.logOut')}
        aria-label={t('auth.logOut')}
      >
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
          <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
          <polyline points="16 17 21 12 16 7" />
          <line x1="21" y1="12" x2="9" y2="12" />
        </svg>
      </button>
    </div>
  );
}
