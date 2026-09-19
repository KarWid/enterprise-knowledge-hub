import {
  createApi,
  fetchBaseQuery,
  retry,
} from "@reduxjs/toolkit/query/react";
import { applyRequestHeaders } from "./requestHeaders";
import type { RootState } from "../store";

const baseQueryWithAuth = fetchBaseQuery({
  baseUrl: import.meta.env.VITE_API_URL,

  prepareHeaders: (headers, { getState }) =>
    applyRequestHeaders(headers, () => getState() as RootState),
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
