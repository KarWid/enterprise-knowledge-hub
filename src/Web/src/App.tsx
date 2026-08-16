import { HealthStatus } from './features/health/HealthStatus';
import { AuthenticatedApp } from './app/AuthenticatedApp';
import { UnauthenticatedApp } from './app/UnauthenticatedApp';
import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react';

function App() {
  return (
    <>
      {/* Health status to remove */}
      <HealthStatus />

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
