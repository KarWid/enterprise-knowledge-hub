import { useTranslation } from 'react-i18next';
import { login } from "../auth/authService";

export function LoginButton() {
  const { t } = useTranslation();

  return (
    <button onClick={login}>
      {t('auth.logIn')}
    </button>
  );
}