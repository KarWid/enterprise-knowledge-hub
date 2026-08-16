import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import { apiScopes } from '../auth/msalConfig';
import { msalInstance } from '../auth/msalInstance';

const baseQueryWithAuth = fetchBaseQuery({
  baseUrl: '/api',
  prepareHeaders: async (headers) => {
    const accounts = msalInstance.getAllAccounts();
    if (accounts.length > 0) {
      try {
        const result = await msalInstance.acquireTokenSilent({
          scopes: apiScopes.backend,
          account: accounts[0],
        });
        headers.set('Authorization', `Bearer ${result.accessToken}`);
      } catch {
        // Silent acquisition failed — user will be prompted on the next interactive request.
      }
    }
    return headers;
  },
});

export const api = createApi({
  reducerPath: 'api',
  baseQuery: baseQueryWithAuth,
  endpoints: (builder) => ({
    getHealth: builder.query<{ status: string; database: string }, void>({
      query: () => '/health',
    }),
    getMe: builder.query<{ id: string; email: string; name: string }, void>({
      query: () => '/me',
    }),
  }),
});

export const { useGetHealthQuery, useGetMeQuery } = api;
