import { useCurrentUser } from "../../services/api/useCurrentUser";
import { LogoutButton } from "../LogoutButton";
import { OrganizationInfo } from "../OrganizationInfo/OrganizationInfo";
import styles from "./UserInfo.module.less";

export function UserInfo() {
  const { data } = useCurrentUser();

  return (
    <div className={styles.wrapper}>
      <OrganizationInfo />
      <div className={styles.avatar} title={data?.name ?? ""}>
        {data?.initials ?? "?"}
      </div>
      <div className={styles.info}>
        {data?.name && <span className={styles.name}>{data.name}</span>}
        {data?.email && <span className={styles.email}>{data.email}</span>}
      </div>
      <LogoutButton className={styles.logoutButton} />
    </div>
  );
}
