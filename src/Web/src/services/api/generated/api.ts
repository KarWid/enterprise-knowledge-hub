import { baseApi as api } from "../baseApi";
const injectedRtkApi = api.injectEndpoints({
  endpoints: (build) => ({
    getHealth: build.query<GetHealthApiResponse, GetHealthApiArg>({
      query: () => ({ url: `/health` }),
    }),
    getMe: build.query<GetMeApiResponse, GetMeApiArg>({
      query: () => ({ url: `/api/me` }),
    }),
    getOrganizations: build.query<
      GetOrganizationsApiResponse,
      GetOrganizationsApiArg
    >({
      query: () => ({ url: `/api/organizations` }),
    }),
    createOrganization: build.mutation<
      CreateOrganizationApiResponse,
      CreateOrganizationApiArg
    >({
      query: (queryArg) => ({
        url: `/api/organizations`,
        method: "POST",
        body: queryArg.createOrganizationRequest,
      }),
    }),
    inviteUserToOrganization: build.mutation<
      InviteUserToOrganizationApiResponse,
      InviteUserToOrganizationApiArg
    >({
      query: (queryArg) => ({
        url: `/api/organizations/${queryArg.organizationId}/invitations`,
        method: "POST",
        body: queryArg.inviteUserRequest,
      }),
    }),
    acceptOrganizationInvitation: build.mutation<
      AcceptOrganizationInvitationApiResponse,
      AcceptOrganizationInvitationApiArg
    >({
      query: (queryArg) => ({
        url: `/api/organizations/invitations/${queryArg.invitationId}/accept`,
        method: "POST",
      }),
    }),
  }),
  overrideExisting: false,
});
export { injectedRtkApi as api };
export type GetHealthApiResponse = unknown;
export type GetHealthApiArg = void;
export type GetMeApiResponse = /** status 200 OK */ CurrentUserResponse;
export type GetMeApiArg = void;
export type GetOrganizationsApiResponse = unknown;
export type GetOrganizationsApiArg = void;
export type CreateOrganizationApiResponse = unknown;
export type CreateOrganizationApiArg = {
  createOrganizationRequest: CreateOrganizationRequest;
};
export type InviteUserToOrganizationApiResponse = unknown;
export type InviteUserToOrganizationApiArg = {
  organizationId: string;
  inviteUserRequest: InviteUserRequest;
};
export type AcceptOrganizationInvitationApiResponse = unknown;
export type AcceptOrganizationInvitationApiArg = {
  invitationId: string;
};
export type UserOnboardingStatus =
  "Complete" | "CreateOrganization" | "AcceptInvitation" | "AccessDenied";
export type MePendingInvitationItem = {
  id?: string;
  organizationId?: string;
  organizationName?: string | null;
  expiresAt?: string;
};
export type OrganizationRole =
  "OrganizationOwner" | "OrganizationAdmin" | "KnowledgeManager" | "Employee";
export type MeOrganizationItem = {
  id?: string;
  name?: string | null;
  role?: OrganizationRole;
};
export type CurrentUserResponse = {
  id?: string;
  email?: string | null;
  name?: string | null;
  onboardingStatus?: UserOnboardingStatus;
  pendingInvitations?: MePendingInvitationItem[] | null;
  organizations?: MeOrganizationItem[] | null;
};
export type CreateOrganizationRequest = {
  name?: string | null;
};
export type InviteUserRequest = {
  email?: string | null;
};
export const {
  useGetHealthQuery,
  useGetMeQuery,
  useGetOrganizationsQuery,
  useCreateOrganizationMutation,
  useInviteUserToOrganizationMutation,
  useAcceptOrganizationInvitationMutation,
} = injectedRtkApi;
