import {
  createApi,
  fetchBaseQuery,
  retry,
  type BaseQueryFn,
  type FetchArgs,
  type FetchBaseQueryError,
} from "@reduxjs/toolkit/query/react";
import { applyRequestHeaders } from "./requestHeaders";
import type { RootState } from "../store";
import { showError } from "../errors/errorSlice";

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

const baseQueryWithErrorHandling: BaseQueryFn<
  string | FetchArgs,
  unknown,
  FetchBaseQueryError
> = async (args, api, extraOptions) => {
  const result = await baseQueryAuthWithRetry(args, api, extraOptions);

  if (result.error) {
    api.dispatch(showError(getErrorMessage(result.error)));
  }

  return result;
};

function getErrorMessage(error: FetchBaseQueryError): string | undefined {
  if (!("data" in error) || typeof error.data !== "object" || error.data === null) {
    return undefined;
  }

  const message = (error.data as { message?: unknown }).message;
  return typeof message === "string" && message.trim().length > 0
    ? message
    : undefined;
}

export const baseApi = createApi({
  reducerPath: "api",
  baseQuery: baseQueryWithErrorHandling,
  endpoints: () => ({}),
});

export const {} = baseApi;
