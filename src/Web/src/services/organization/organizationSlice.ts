import { createSlice, type PayloadAction } from "@reduxjs/toolkit";
import type { RootState } from "../store";

const STORAGE_KEY = "ekh.currentOrganizationId";

function readStoredOrganizationId(): string | null {
  try {
    return localStorage.getItem(STORAGE_KEY);
  } catch {
    return null;
  }
}

function writeStoredOrganizationId(organizationId: string): void {
  try {
    localStorage.setItem(STORAGE_KEY, organizationId);
  } catch {
    // ignore storage errors (e.g. private browsing / disabled storage)
  }
}

interface OrganizationState {
  currentOrganizationId: string | null;
}

const initialState: OrganizationState = {
  currentOrganizationId: readStoredOrganizationId(),
};

const organizationSlice = createSlice({
  name: "organization",
  initialState,
  reducers: {
    setCurrentOrganizationId(state, action: PayloadAction<string>) {
      state.currentOrganizationId = action.payload;
      writeStoredOrganizationId(action.payload);
    },
  },
});

export const { setCurrentOrganizationId } = organizationSlice.actions;
export const organizationReducer = organizationSlice.reducer;

export const selectCurrentOrganizationId = (state: RootState): string | null =>
  state.organization.currentOrganizationId;
