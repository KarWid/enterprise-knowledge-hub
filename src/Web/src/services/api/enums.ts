import type { UserOnboardingStatus } from "./generated/api";

export const UserOnboardingStatusType = {
  Complete: "Complete",
  CreateOrganization: "CreateOrganization",
  AcceptInvitation: "AcceptInvitation",
  AccessDenied: "AccessDenied",
} as const satisfies Record<string, UserOnboardingStatus>;
