import React from "react";
import ReactDOM from "react-dom/client";
import { MsalProvider } from "@azure/msal-react";
import { Provider } from "react-redux";
import { BrowserRouter } from "react-router-dom";
import { store } from "./services/store";

import App from "./App";
import { msalInstance } from "./auth/msalInstance";
import { initializeAuth } from "./auth/authBootstrap";
import "./i18n";

async function bootstrap(): Promise<void> {
  await initializeAuth();

  ReactDOM.createRoot(
    document.getElementById("root")!
  ).render(
    <React.StrictMode>
      <MsalProvider instance={msalInstance}>  
        <Provider store={store}>
          <BrowserRouter>
            <App />
          </BrowserRouter>
        </Provider>
      </MsalProvider>
    </React.StrictMode>
  );
}

bootstrap();