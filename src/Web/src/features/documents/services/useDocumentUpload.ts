import { useState } from "react";
import {
  useBeginDocumentUploadMutation,
  useCompleteDocumentUploadMutation,
} from "../../../services/api/generated/api";

interface UseDocumentUploadOptions {
  onCompleted: () => void;
}

export function useDocumentUpload({ onCompleted }: UseDocumentUploadOptions) {
  const [file, setFile] = useState<File | null>(null);
  const [storageUploadError, setStorageUploadError] = useState<unknown>();
  const [isUploadingToStorage, setIsUploadingToStorage] = useState(false);
  const [
    beginDocumentUpload,
    { error: beginUploadError, isLoading: isBeginningUpload },
  ] = useBeginDocumentUploadMutation();
  const [
    completeDocumentUpload,
    { error: completeUploadError, isLoading: isCompletingUpload },
  ] = useCompleteDocumentUploadMutation();

  const isUploading =
    isBeginningUpload || isUploadingToStorage || isCompletingUpload;
  const error = storageUploadError ?? beginUploadError ?? completeUploadError;

  async function uploadSelectedFile() {
    if (file === null) return;

    setStorageUploadError(undefined);
    const reservation = await beginDocumentUpload({
      createDocumentUploadRequest: { fileName: file.name },
    });
    if ("error" in reservation) return;

    setIsUploadingToStorage(true);
    try {
      const response = await fetch(reservation.data.uploadUri, {
        method: "PUT",
        headers: {
          "If-None-Match": "*",
          "x-ms-blob-content-type": "application/pdf",
          "x-ms-blob-type": "BlockBlob",
          "x-ms-version": "2023-11-03",
        },
        body: file,
      });

      if (!response.ok) {
        setStorageUploadError({
          data: {
            message: `Storage upload failed with status ${response.status}.`,
          },
        });
        return;
      }
    } catch {
      setStorageUploadError({
        data: {
          message: "The browser could not upload the document to storage.",
        },
      });
      return;
    } finally {
      setIsUploadingToStorage(false);
    }

    // TODO @KWidla: think how to handle retries for failed storage uploads before completing the document upload.
    const result = await completeDocumentUpload({
      documentId: reservation.data.documentId,
    });
    if (!("error" in result)) {
      setFile(null);
      onCompleted();
    }
  }

  function retryUpload() {
    if (error !== undefined && file !== null) {
      void uploadSelectedFile();
    }
  }

  return {
    file,
    setFile,
    isUploading,
    error,
    uploadSelectedFile,
    retryUpload,
  };
}
