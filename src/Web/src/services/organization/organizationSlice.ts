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
  switchingLocks: string[];
}

const initialState: OrganizationState = {
  currentOrganizationId: readStoredOrganizationId(),
  switchingLocks: [],
};

const organizationSlice = createSlice({
  name: "organization",
  initialState,
  reducers: {
    setCurrentOrganizationId(state, action: PayloadAction<string>) {
      if (state.switchingLocks.length > 0) return;
      state.currentOrganizationId = action.payload;
      writeStoredOrganizationId(action.payload);
    },
    lockOrganizationSwitching(state, action: PayloadAction<string>) {
      if (!state.switchingLocks.includes(action.payload)) {
        state.switchingLocks.push(action.payload);
      }
    },
    unlockOrganizationSwitching(state, action: PayloadAction<string>) {
      state.switchingLocks = state.switchingLocks.filter(id => id !== action.payload);
    },
  },
});

export const {
  setCurrentOrganizationId,
  lockOrganizationSwitching,
  unlockOrganizationSwitching,
} = organizationSlice.actions;
export const organizationReducer = organizationSlice.reducer;

export const selectCurrentOrganizationId = (state: RootState): string | null =>
  state.organization.currentOrganizationId;

export const selectOrganizationSwitchingLocked = (state: RootState): boolean =>
  state.organization.switchingLocks.length > 0;
