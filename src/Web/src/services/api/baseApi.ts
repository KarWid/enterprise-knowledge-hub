import { createApi, fetchBaseQuery, retry } from "@reduxjs/toolkit/query/react";
import { apiScopes } from "../../auth/msalConfig";
import { msalInstance } from "../../auth/msalInstance";

const baseQueryWithAuth = fetchBaseQuery({
  baseUrl: `${import.meta.env.VITE_API_URL}`,
  prepareHeaders: async (headers) => {
    const account = msalInstance.getActiveAccount();

    if (!account) {
      return headers;
    }

    try {
      const result = await msalInstance.acquireTokenSilent({
        scopes: apiScopes.backend,
        account: account,
      });

      headers.set("Authorization", `Bearer ${result.accessToken}`);
    } catch (error) {
      console.error("Failed to acquire token silently", error);
    }

    return headers;
  },
});

const baseQueryAuthWithRetry = retry(
  async (args, api, extraOptions) => {
    const result = await baseQueryWithAuth(args, api, extraOptions);

    if (result.error?.status === 401) {
      retry.fail(result.error, result.meta);
    }

    return result;
  },
  { maxRetries: 3 },
);

export const baseApi = createApi({
  reducerPath: "api",
  baseQuery: baseQueryAuthWithRetry,
  endpoints: () => ({}),
});

export const {} = baseApi;
