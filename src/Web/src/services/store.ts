import { configureStore } from "@reduxjs/toolkit";
import { api } from "./api/generated/api";
import { organizationReducer } from "./organization/organizationSlice";

export const store = configureStore({
  reducer: {
    [api.reducerPath]: api.reducer,
    organization: organizationReducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(api.middleware),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
