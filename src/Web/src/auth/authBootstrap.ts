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

  removeAuthCallbackParameters();
}

function removeAuthCallbackParameters(): void {
  const url = new URL(window.location.href);

  const authParameters = [
    "code",
    "state",
    "session_state",
    "error",
    "error_description",
    "error_uri",
  ];

  let urlChanged = false;

  for (const parameter of authParameters) {
    if (url.searchParams.has(parameter)) {
      url.searchParams.delete(parameter);
      urlChanged = true;
    }
  }

  if (!urlChanged) {
    return;
  }

  const cleanUrl = `${url.pathname}${url.search}${url.hash}`;

  window.history.replaceState(window.history.state, document.title, cleanUrl);
}
