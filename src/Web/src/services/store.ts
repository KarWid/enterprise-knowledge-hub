import { configureStore } from "@reduxjs/toolkit";
import { api } from "./api/generated/api";
import { errorReducer } from "./errors/errorSlice";
import { organizationReducer } from "./organization/organizationSlice";

export const store = configureStore({
  reducer: {
    [api.reducerPath]: api.reducer,
    error: errorReducer,
    organization: organizationReducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(api.middleware),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
