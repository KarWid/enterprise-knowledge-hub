import { useTranslation } from 'react-i18next';
import { LoginButton } from '../components/LoginButton';

export function UnauthenticatedApp() {
  const { t } = useTranslation();

  return (
    <main>
      <h1>{t('app.title')}</h1>

      <p>{t('auth.signInPrompt')}</p>

      <LoginButton />
    </main>
  );
}