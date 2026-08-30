import { useGetMeQuery } from '../../services/api/generated/api';
import { LogoutButton } from '../LogoutButton';
import styles from './UserInfo.module.less';

export function UserInfo() {
  const { data } = useGetMeQuery();

  const initials = data?.name
    ? data.name.split(' ').map((n: string) => n[0]).slice(0, 2).join('').toUpperCase()
    : '?';

  return (
    <div className={styles.wrapper}>
      <div className={styles.avatar} title={data?.name ?? ''}>
        {initials}
      </div>
      <div className={styles.info}>
        {data?.name && <span className={styles.name}>{data.name}</span>}
        {data?.email && <span className={styles.email}>{data.email}</span>}
      </div>
      <LogoutButton className={styles.logoutButton} />
    </div>
  );
}
