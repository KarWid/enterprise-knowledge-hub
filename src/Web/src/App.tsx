import { useMsal } from '@azure/msal-react';
import { InteractionStatus } from "@azure/msal-browser";
import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react';
import { AuthLoadingPage } from './app/AuthLoadingPage';
import { AuthenticatedApp } from './app/AuthenticatedApp';
import { UnauthenticatedApp } from './app/UnauthenticatedApp';
import { useTranslation } from 'react-i18next';

function App() {
  const { t } = useTranslation();
  const { inProgress } = useMsal();

  if (inProgress !== InteractionStatus.None) {
    return <AuthLoadingPage message={t('app.pleaseWait')} />;
  }

  return (
    <>
      <UnauthenticatedTemplate>
        <UnauthenticatedApp />
      </UnauthenticatedTemplate>

      <AuthenticatedTemplate>
        <AuthenticatedApp />
      </AuthenticatedTemplate>
    </>
  );
}

export default App
