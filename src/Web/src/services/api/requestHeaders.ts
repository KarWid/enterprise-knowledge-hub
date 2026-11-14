import { apiScopes } from "../../auth/msalConfig";
import { msalInstance } from "../../auth/msalInstance";
import { selectCurrentOrganizationId } from "../organization/organizationSlice";
import type { RootState } from "../store";

export async function applyRequestHeaders(
  headers: Headers,
  getState: () => RootState,
): Promise<Headers> {
  await addAuthorizationHeader(headers);
  addOrganizationHeader(headers, getState);

  return headers;
}

async function addAuthorizationHeader(headers: Headers): Promise<void> {
  const account = msalInstance.getActiveAccount();

  if (!account) {
    return;
  }

  try {
    const result = await msalInstance.acquireTokenSilent({
      scopes: apiScopes.backend,
      account,
    });

    headers.set("Authorization", `Bearer ${result.accessToken}`);
  } catch (error) {
    console.error("Failed to acquire token silently", error);
  }
}

function addOrganizationHeader(
  headers: Headers,
  getState: () => RootState,
): void {
  const organizationId = selectCurrentOrganizationId(getState());

  // Generated document endpoints carry an explicit Organization in their query
  // cache key. Preserve it if selection changes while token acquisition awaits.
  if (organizationId && !headers.has("X-Organization-Id")) {
    headers.set("X-Organization-Id", organizationId);
  }
}
