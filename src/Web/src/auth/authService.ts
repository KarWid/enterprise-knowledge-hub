import type {
  AccountInfo
} from "@azure/msal-browser";

import { msalInstance } from "./msalInstance";
import { apiScopes, msalConfig } from "./msalConfig";

export const login = async () => {
  await msalInstance.loginRedirect({
    scopes: apiScopes.login,
  });

  const accounts = msalInstance.getAllAccounts();

  if (accounts.length > 0) {
    msalInstance.setActiveAccount(accounts[0]);
  }
};

export const logout = async () => {
  const account = msalInstance.getActiveAccount();

  await msalInstance.logoutRedirect({
    account,
    postLogoutRedirectUri: msalConfig.auth.redirectUri,
  });
};

export const getActiveAccount = (): AccountInfo | null => {
  return msalInstance.getActiveAccount();
};