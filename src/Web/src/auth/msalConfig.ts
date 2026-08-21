import { Configuration } from '@azure/msal-browser';

export const msalConfig: Configuration = {
  auth: {
    clientId: import.meta.env.VITE_ENTRA_CLIENT_ID ?? '',
    authority: import.meta.env.VITE_ENTRA_AUTHORITY ?? '',
    knownAuthorities: [
      new URL(import.meta.env.VITE_ENTRA_AUTHORITY).host,
      import.meta.env.VITE_ENTRA_ISSUER_HOST,
    ],
    redirectUri: import.meta.env.VITE_ENTRA_REDIRECT_URI ?? ''
  },
  cache: {
    cacheLocation: 'sessionStorage',
  }
};

// Scopes requested when acquiring a token for the API.
export const apiScopes = {
  login: ["openid", "profile"],
  backend: [import.meta.env.VITE_ENTRA_API_CLIENT_ID ?? '']
}
