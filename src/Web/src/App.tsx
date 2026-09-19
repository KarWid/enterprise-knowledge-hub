import { useMsal } from '@azure/msal-react';
import { InteractionStatus } from "@azure/msal-browser";
import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react';
import { AuthLoadingPage } from './app/AuthLoadingPage';
import { AuthenticatedApp } from './app/AuthenticatedApp';
import { UnauthenticatedApp } from './app/UnauthenticatedApp';
import { useTranslation } from 'react-i18next';
import { ErrorModal } from "./components/ErrorModal/ErrorModal";
import { clearError, selectErrorMessage } from "./services/errors/errorSlice";
import { useAppDispatch, useAppSelector } from "./services/hooks";

function App() {
  const { t } = useTranslation();
  const { inProgress } = useMsal();
  const dispatch = useAppDispatch();
  const errorMessage = useAppSelector(selectErrorMessage);

  const modalMessage = errorMessage || t("errorModal.genericMessage");

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

      <ErrorModal
        message={errorMessage === null ? null : modalMessage}
        onClose={() => dispatch(clearError())}
      />
    </>
  );
}

export default App
