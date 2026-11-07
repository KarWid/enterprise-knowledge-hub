import { useCallback } from "react";
import type { TFunction } from "i18next";
import { useTranslation } from "react-i18next";
import {
  type DocumentUploadCapabilitiesResponse,
  useGetDocumentsQuery,
  useGetDocumentUploadCapabilitiesQuery,
} from "../../../services/api/generated/api";
import { isValidUploadFile } from "../utils/isValidUploadFile";
import { useDocumentUpload } from "./useDocumentUpload";

export function useDocumentsPage() {
  const { t } = useTranslation();
  const documentsQuery = useGetDocumentsQuery(undefined, { refetchOnMountOrArgChange: true });
  const capabilitiesQuery = useGetDocumentUploadCapabilitiesQuery(undefined, { refetchOnMountOrArgChange: true });
  const { refetch: refetchDocuments } = documentsQuery;
  const refreshDocuments = useCallback(() => { void refetchDocuments(); }, [refetchDocuments]);
  const upload = useDocumentUpload({ onCompleted: refreshDocuments });

  const capabilities = capabilitiesQuery.data;
  const formats = capabilities?.formats ?? [];
  const hasEnabledFormats = formats.length > 0;
  const canSelectFile = capabilitiesQuery.error === undefined && hasEnabledFormats;
  const hasSelectedFile = upload.file !== null;
  const selectedFileIsValid =
    upload.file !== null &&
    capabilities !== undefined &&
    isValidUploadFile(upload.file, capabilities);
  const canSubmit = canSelectFile && selectedFileIsValid && !upload.isUploading;

  function submitUpload() {
    if (canSubmit) {
      void upload.uploadSelectedFile();
    }
  }

  function retry() {
    if (capabilitiesQuery.error !== undefined) {
      void capabilitiesQuery.refetch();
      return;
    }

    if (upload.error !== undefined && hasSelectedFile) {
      submitUpload();
      return;
    }

    refreshDocuments();
  }

  return {
    documents: documentsQuery.data ?? [],
    isLoading: documentsQuery.isLoading,
    error: capabilitiesQuery.error ?? upload.error ?? documentsQuery.error,
    retry,
    upload: {
      file: upload.file,
      selectFile: upload.setFile,
      isUploading: upload.isUploading,
      notice: upload.notice,
      description: getUploadDescription(capabilities, t),
      accept: formats.flatMap(format => [format.extension, format.contentType]).join(","),
      canSelectFile: canSelectFile && !upload.isUploading,
      canSubmit,
      showValidationError: hasSelectedFile && canSelectFile && !selectedFileIsValid,
      submit: submitUpload,
    },
  };
}

function getUploadDescription(
  capabilities: DocumentUploadCapabilitiesResponse | undefined,
  t: TFunction,
): string {
  if (capabilities === undefined) return t("documents.loadingFormats");
  if (capabilities.formats.length === 0) return t("documents.uploadsDisabled");

  return t("documents.uploadDescription", {
    formats: capabilities.formats.map(format => format.extension).join(", "),
    maxSize: capabilities.maxSizeBytes / (1024 * 1024),
  });
}
