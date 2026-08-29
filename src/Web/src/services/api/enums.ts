import type { UserOnboardingStatus } from './generated/api';

export const UserOnboardingStatusType = {
  Complete: 'Complete',
  CreateOrganization: 'CreateOrganization',
  AcceptInvitation: 'AcceptInvitation',
} as const satisfies Record<
  string,
  UserOnboardingStatus
>;