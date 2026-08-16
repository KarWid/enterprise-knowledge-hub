import { useTranslation } from 'react-i18next';
import { LogoutButton } from '../components/LogoutButton';

export function AuthenticatedApp() {
  const { t } = useTranslation();

  return (
    <div>
      <header>
        <h1>{t('app.title')}</h1>

        <LogoutButton />
      </header>

      <main>
        <h2>{t('authenticated.welcome')}</h2>

        {/* TODO: routing here */}
      </main>
    </div>
  );
}