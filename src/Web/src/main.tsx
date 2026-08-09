import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { MsalProvider } from '@azure/msal-react'
import { Provider } from 'react-redux'
import { store } from './services/store'
import { msalInstance } from './auth/msalConfig'
import App from './App.tsx'

msalInstance.initialize().then(() => {
  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <MsalProvider instance={msalInstance}>
        <Provider store={store}>
          <App />
        </Provider>
      </MsalProvider>
    </StrictMode>,
  );
});
