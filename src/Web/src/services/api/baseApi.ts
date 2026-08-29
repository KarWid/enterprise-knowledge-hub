import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import { apiScopes } from '../../auth/msalConfig';
import { msalInstance } from '../../auth/msalInstance';

const baseQueryWithAuth = fetchBaseQuery({
  baseUrl: `${import.meta.env.VITE_API_URL}/api`,
  prepareHeaders: async (headers) => {
    const account = msalInstance.getActiveAccount();

    if (!account){
      return headers;
    }

    try{
      const result = await msalInstance.acquireTokenSilent({
        scopes: apiScopes.backend,
        account: account,
      });

      headers.set('Authorization', `Bearer ${result.accessToken}`);
    } catch (error) {
      console.error('Failed to acquire token silently', error);
    }

    return headers;
  },
});

export const baseApi = createApi({
  reducerPath: 'api',
  baseQuery: baseQueryWithAuth,
  endpoints: () => ({}),
});

export const { } = baseApi;
