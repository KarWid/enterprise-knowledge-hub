import { Configuration, PublicClientApplication } from '@azure/msal-browser';

const msalConfig: Configuration = {
  auth: {
    clientId: import.meta.env.VITE_ENTRA_CLIENT_ID ?? '',
    authority: import.meta.env.VITE_ENTRA_AUTHORITY ?? '',
    redirectUri: window.location.origin,
  },
  cache: {
    cacheLocation: 'sessionStorage',
  },
};

export const msalInstance = new PublicClientApplication(msalConfig);

// Scopes requested when acquiring a token for the API.
export const apiScopes: string[] = [import.meta.env.VITE_API_SCOPE ?? ''];
