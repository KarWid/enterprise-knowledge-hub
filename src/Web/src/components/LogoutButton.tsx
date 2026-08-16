import { useTranslation } from 'react-i18next';
import { logout } from "../auth/authService";

export function LogoutButton() {
  const { t } = useTranslation();

  return (
    <button onClick={logout}>
      {t('auth.logOut')}
    </button>
  );
}