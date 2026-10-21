import { useEffect, useRef, useState } from "react";
import {
  useBeginDocumentUploadMutation,
  useGetDocumentQuery,
} from "../../../services/api/generated/api";

interface UseDocumentUploadOptions {
  onCompleted: () => void;
}

export function useDocumentUpload({ onCompleted }: UseDocumentUploadOptions) {
  const [file, setFile] = useState<File | null>(null);
  const [storageUploadError, setStorageUploadError] = useState<unknown>();
  const [isUploadingToStorage, setIsUploadingToStorage] = useState(false);
  const [documentId, setDocumentId] = useState<string>();
  const [pollingExpired, setPollingExpired] = useState(false);
  const [notice, setNotice] = useState<
    "verifying" | "verified" | "rejected" | "failed" | "pending"
  >();
  const uploadController = useRef<AbortController | null>(null);
  const mounted = useRef(true);
  const [
    beginDocumentUpload,
    { error: beginUploadError, isLoading: isBeginningUpload },
  ] = useBeginDocumentUploadMutation();

  const { data: document } = useGetDocumentQuery(
    { documentId: documentId ?? "" },
    {
      skip: !documentId || pollingExpired || notice !== "verifying",
      pollingInterval: 2000,
      refetchOnMountOrArgChange: true,
    },
  );

  useEffect(() => {
    mounted.current = true;

    return () => {
      mounted.current = false;
      uploadController.current?.abort();
    };
  }, []);

  useEffect(() => {
    if (!isUploadingToStorage) return;

    function warnBeforeLeaving(event: BeforeUnloadEvent) {
      event.preventDefault();
      event.returnValue = true;
    }

    window.addEventListener("beforeunload", warnBeforeLeaving);

    return () => window.removeEventListener("beforeunload", warnBeforeLeaving);
  }, [isUploadingToStorage]);

  useEffect(() => {
    if (!documentId || notice !== "verifying") return;

    const timer = window.setTimeout(() => {
      setPollingExpired(true);
      setNotice("pending"); // Server verification continues; timeout is NOT upload failure.
    }, 120_000);

    return () => window.clearTimeout(timer);
  }, [documentId, notice]);

  useEffect(() => {
    if (
      !document ||
      document.id !== documentId ||
      document.status === "PendingForUpload"
    ) {
      return;
    }

    setNotice(
      document.status === "Rejected"
        ? "rejected"
        : document.status === "Failed"
          ? "failed"
          : "verified",
    );

    setDocumentId(undefined);
    onCompleted();
  }, [document, documentId, onCompleted]);

  async function uploadSelectedFile() {
    if (!file || isBeginningUpload || isUploadingToStorage) {
      return;
    }

    setStorageUploadError(undefined);
    setNotice(undefined);
    setDocumentId(undefined);

    const reservation = await beginDocumentUpload({
      createDocumentUploadRequest: { fileName: file.name },
    });

    if (!mounted.current || "error" in reservation) {
      return;
    }

    const controller = new AbortController();
    uploadController.current = controller;
    setIsUploadingToStorage(true);

    try {
      // A single Put Blob works with sp=c. Do not replace with chunked Put Block/Put Block List.
      const response = await fetch(reservation.data.uploadUri, {
        method: "PUT",
        headers: {
          "If-None-Match": "*",
          "x-ms-blob-content-type": "application/pdf",
          "x-ms-blob-type": "BlockBlob",
          "x-ms-version": "2023-11-03",
        },
        body: file,
        signal: controller.signal,
      });
      if (!mounted.current) return;
      if (!response.ok) {
        setStorageUploadError({
          data: {
            message: `Storage upload failed with status ${response.status}.`,
          },
        });
        return;
      }

      setFile(null);
      setDocumentId(reservation.data.documentId);
      setPollingExpired(false);
      setNotice("verifying");
      onCompleted();
    } catch {
      if (mounted.current && !controller.signal.aborted)
        setStorageUploadError({
          data: {
            message:
              "The browser could not confirm the upload. Check the document list before trying again.",
          },
        });
    } finally {
      if (uploadController.current === controller) {
        uploadController.current = null;
      }

      if (mounted.current) {
        setIsUploadingToStorage(false);
      }
    }
  }

  return {
    file,
    setFile,
    notice,
    isUploading: isBeginningUpload || isUploadingToStorage,
    error: storageUploadError ?? beginUploadError,
    uploadSelectedFile,
    retryUpload: () => {
      void uploadSelectedFile();
    },
  };
}
