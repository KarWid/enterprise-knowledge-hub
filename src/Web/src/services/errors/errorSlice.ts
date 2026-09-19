import { createSlice, type PayloadAction } from "@reduxjs/toolkit";
import type { RootState } from "../store";

interface ErrorState {
  message: string | null;
}

const initialState: ErrorState = {
  message: null,
};

const errorSlice = createSlice({
  name: "error",
  initialState,
  reducers: {
    showError(state, action: PayloadAction<string | undefined>) {
      state.message = action.payload ?? "";
    },
    clearError(state) {
      state.message = null;
    },
  },
});

export const { clearError, showError } = errorSlice.actions;
export const errorReducer = errorSlice.reducer;

export const selectErrorMessage = (state: RootState): string | null =>
  state.error.message;
