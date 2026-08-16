import { msalInstance } from "./msalInstance";

export async function initializeAuth() {
  await msalInstance.initialize();

  const response = await msalInstance.handleRedirectPromise();

  if (response?.account) {
    msalInstance.setActiveAccount(response.account);
    return;
  }

  const accounts = msalInstance.getAllAccounts();

  if (accounts.length === 1) {
    msalInstance.setActiveAccount(accounts[0]);
  }
}