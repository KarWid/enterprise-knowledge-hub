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
export type UserOnboardingStatus =
  "Complete" | "CreateOrganization" | "AcceptInvitation";
export type CurrentUserResponse = {
  id?: string;
  email?: string | null;
  name?: string | null;
  onboardingStatus?: UserOnboardingStatus;
};
export type CreateOrganizationRequest = {
  name?: string | null;
};
export const {
  useGetHealthQuery,
  useGetMeQuery,
  useGetOrganizationsQuery,
  useCreateOrganizationMutation,
} = injectedRtkApi;
