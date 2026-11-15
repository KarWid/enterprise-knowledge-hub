import { baseApi as api } from "../baseApi";
const injectedRtkApi = api.injectEndpoints({
  endpoints: (build) => ({
    getDocumentUploadCapabilities: build.query<
      GetDocumentUploadCapabilitiesApiResponse,
      GetDocumentUploadCapabilitiesApiArg
    >({
      query: (queryArg) => ({
        url: `/api/documents/upload-capabilities`,
        headers: {
          "X-Organization-Id": queryArg["X-Organization-Id"],
        },
      }),
    }),
    getDocuments: build.query<GetDocumentsApiResponse, GetDocumentsApiArg>({
      query: (queryArg) => ({
        url: `/api/documents`,
        headers: {
          "X-Organization-Id": queryArg["X-Organization-Id"],
        },
      }),
    }),
    beginDocumentUpload: build.mutation<
      BeginDocumentUploadApiResponse,
      BeginDocumentUploadApiArg
    >({
      query: (queryArg) => ({
        url: `/api/documents/uploads`,
        method: "POST",
        body: queryArg.createDocumentUploadRequest,
        headers: {
          "X-Organization-Id": queryArg["X-Organization-Id"],
        },
      }),
    }),
    getDocument: build.query<GetDocumentApiResponse, GetDocumentApiArg>({
      query: (queryArg) => ({
        url: `/api/documents/${queryArg.documentId}`,
        headers: {
          "X-Organization-Id": queryArg["X-Organization-Id"],
        },
      }),
    }),
    getDocumentChunks: build.query<
      GetDocumentChunksApiResponse,
      GetDocumentChunksApiArg
    >({
      query: (queryArg) => ({
        url: `/api/documents/${queryArg.documentId}/chunks`,
        headers: {
          "X-Organization-Id": queryArg["X-Organization-Id"],
        },
        params: {
          offset: queryArg.offset,
          limit: queryArg.limit,
        },
      }),
    }),
    getDocumentTextPreview: build.query<
      GetDocumentTextPreviewApiResponse,
      GetDocumentTextPreviewApiArg
    >({
      query: (queryArg) => ({
        url: `/api/documents/${queryArg.documentId}/text-preview`,
        headers: {
          "X-Organization-Id": queryArg["X-Organization-Id"],
        },
      }),
    }),
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
export type GetDocumentUploadCapabilitiesApiResponse =
  /** status 200 OK */ DocumentUploadCapabilitiesResponse;
export type GetDocumentUploadCapabilitiesApiArg = {
  /** Organization selected for this request. Access is validated by the API. */
  "X-Organization-Id": string;
};
export type GetDocumentsApiResponse = /** status 200 OK */ DocumentResponse[];
export type GetDocumentsApiArg = {
  /** Organization selected for this request. Access is validated by the API. */
  "X-Organization-Id": string;
};
export type BeginDocumentUploadApiResponse =
  /** status 201 Created */ DocumentUploadSessionResponse;
export type BeginDocumentUploadApiArg = {
  /** Organization selected for this request. Access is validated by the API. */
  "X-Organization-Id": string;
  createDocumentUploadRequest: CreateDocumentUploadRequest;
};
export type GetDocumentApiResponse = /** status 200 OK */ DocumentResponse;
export type GetDocumentApiArg = {
  documentId: string;
  /** Organization selected for this request. Access is validated by the API. */
  "X-Organization-Id": string;
};
export type GetDocumentChunksApiResponse =
  /** status 200 OK */ DocumentChunksResponse;
export type GetDocumentChunksApiArg = {
  documentId: string;
  offset?: number;
  limit?: number;
  /** Organization selected for this request. Access is validated by the API. */
  "X-Organization-Id": string;
};
export type GetDocumentTextPreviewApiResponse =
  /** status 200 OK */ DocumentTextPreviewResponse;
export type GetDocumentTextPreviewApiArg = {
  documentId: string;
  /** Organization selected for this request. Access is validated by the API. */
  "X-Organization-Id": string;
};
export type GetHealthApiResponse = /** status 200 OK */ HealthResponse;
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
export type DocumentUploadFormatResponse = {
  id: string;
  extension: string;
  contentType: string;
};
export type DocumentUploadCapabilitiesResponse = {
  formats: DocumentUploadFormatResponse[];
  maxSizeBytes: number;
};
export type ErrorResult = {
  code?: string | null;
  message?: string | null;
};
export type DocumentTextExtractionResponse = {
  status: string;
  characterCount?: number | null;
  completedAt?: string | null;
  failureCode?: string | null;
};
export type DocumentChunkingResponse = {
  status: string;
  chunkCount?: number | null;
  completedAt?: string | null;
  failureCode?: string | null;
};
export type DocumentResponse = {
  id: string;
  name: string;
  contentType: string;
  status: string;
  createdAt: string;
  textExtraction?: DocumentTextExtractionResponse;
  chunking?: DocumentChunkingResponse;
};
export type DocumentUploadSessionResponse = {
  documentId: string;
  uploadUri: string;
  expiresAt: string;
  contentType: string;
};
export type CreateDocumentUploadRequest = {
  fileName?: string | null;
};
export type ProblemDetails = {
  type?: string | null;
  title?: string | null;
  status?: number | null;
  detail?: string | null;
  instance?: string | null;
  [key: string]: any;
};
export type DocumentChunkPreviewResponse = {
  id: string;
  ordinal: number;
  content: string;
  headingPath?: string | null;
  tokenCount: number;
  isTruncated: boolean;
};
export type DocumentChunksResponse = {
  items: DocumentChunkPreviewResponse[];
  chunkCount: number;
  offset: number;
  hasMore: boolean;
};
export type DocumentTextPreviewResponse = {
  text: string;
  characterCount: number;
  isTruncated: boolean;
  completedAt: string;
};
export type HealthResponse = {
  status?: string | null;
  database?: string | null;
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
  useGetDocumentUploadCapabilitiesQuery,
  useGetDocumentsQuery,
  useBeginDocumentUploadMutation,
  useGetDocumentQuery,
  useGetDocumentChunksQuery,
  useGetDocumentTextPreviewQuery,
  useGetHealthQuery,
  useGetMeQuery,
  useGetOrganizationsQuery,
  useCreateOrganizationMutation,
  useInviteUserToOrganizationMutation,
  useAcceptOrganizationInvitationMutation,
} = injectedRtkApi;
